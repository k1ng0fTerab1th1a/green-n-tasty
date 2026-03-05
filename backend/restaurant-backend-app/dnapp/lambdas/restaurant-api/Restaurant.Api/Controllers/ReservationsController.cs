using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Models;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Api.Controllers
{
    [ApiController]
    [Route("reservations")]
    [Authorize]
    public sealed class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IAuthService _authService;

        public ReservationsController(IReservationService reservationService, IAuthService authService)
        {
            _reservationService = reservationService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            var actorUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(actorUserId))
                return ApiResponse<object>.Fail(StatusCodes.Status401Unauthorized, "Unauthorized.");

            var actorIsWaiter = _authService.IsWaiter(User);

            var items = await _reservationService.GetMyAsync(actorUserId, actorIsWaiter, ct);
            return ApiResponse<IReadOnlyList<Reservation>>.Success(StatusCodes.Status200OK, items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(actorUserId))
                return ApiResponse<object>.Fail(StatusCodes.Status401Unauthorized, "Unauthorized.");

            var actorIsWaiter = User.IsInRole("Waiter");
            var item = await _reservationService.GetByIdAsync(id, actorUserId, actorIsWaiter, ct);

            if (item is null)
                return ApiResponse<object>.Fail(StatusCodes.Status404NotFound, "Reservation not found.");

            return ApiResponse<Reservation>.Success(StatusCodes.Status200OK, item);
        }
    }
}
