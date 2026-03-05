using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Models;
using Restaurant.Api.Models.Mappers;
using Restaurant.Core.Interfaces;
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

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            var actorUserId = User.GetUserId();

            var actorIsWaiter = User.IsWaiter();

            var items = await _reservationService.GetMyAsync(actorUserId, actorIsWaiter, ct);

            var dto = items.Select(x => x.ToResponse()).ToList();
            return ApiResponse<List<ReservationResponse>>.Success(StatusCodes.Status200OK, dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var actorUserId = User.GetUserId();

            var actorIsWaiter = User.IsWaiter();

            var entity = await _reservationService.GetByIdAsync(id, actorUserId, actorIsWaiter, ct);
            if (entity is null)
                return ApiResponse<object>.Fail(StatusCodes.Status404NotFound, "Reservation not found.");

            return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, entity.ToResponse());
        }
    }
}
