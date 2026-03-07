using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Api.Models;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers
{
    [ApiController]
    [Route("reservations")]
    //[Authorize]
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            var actorUserId = User.GetUserId();
            if (string.IsNullOrEmpty(actorUserId))
            {
                return Unauthorized("You must log in to proceed with actions to the reservation");
            }
            var actorIsWaiter = User.IsWaiter();
            var result = await _reservationService.CancelReservation(id,actorUserId, actorIsWaiter, ct);
            if (result) return Ok();
            return BadRequest("Deletion is unsuccessful");
        }

        [HttpPost("client")]
        public async Task<IActionResult> CreateForClient([FromBody] CreateReservationRequest request, CancellationToken ct)
        {
            var customerId = User.GetUserId();
            var reservationEntity = await _reservationService.CreateForClientAsync(customerId, request.ToCreateDTO(), ct);
            if (reservationEntity is null)
                return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "Failed to create reservation.");

            return ApiResponse<ReservationResponse>.Success(StatusCodes.Status201Created, reservationEntity.ToResponse());
        }
    }
}
