using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

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
    [ProducesResponseType(typeof(ApiResponse<List<ReservationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var actorUserId = User.GetUserId();

        var actorIsWaiter = User.IsWaiter();

        var items = await _reservationService.GetMyAsync(actorUserId, actorIsWaiter, ct);

        var dto = items.Select(x => x.ToResponse()).ToList();
        return ApiResponse<List<ReservationResponse>>.Success(StatusCodes.Status200OK, dto);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var actorIsWaiter = User.IsWaiter();
        var result = await _reservationService.CancelReservation(id, actorUserId, actorIsWaiter, ct);
        if (result)
        {
            return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Reservation cancelled successfully.");
        }

        return ApiResponse<object>.Fail(StatusCodes.Status500InternalServerError,
            "During reservation cancellation something went wrong");
    }

    [HttpPost("client")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateForClient([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var customerId = User.GetUserId();
        var reservationEntity = await _reservationService.CreateForClientAsync(customerId, request.ToCreateDTO(), ct);
        if (reservationEntity is null)
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "Failed to create reservation.");

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status201Created, reservationEntity.ToResponse());
    }

    [HttpGet("waiter/customers")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<WaiterCustomerLookupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchCustomersForWaiter([FromQuery] string query, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var actorUserId = User.GetUserId();
        var customers = await _reservationService.SearchCustomersForWaiterAsync(actorUserId, query, ct);

        var response = customers
            .Select(x => x.ToWaiterCustomerLookupResponse())
            .ToList();

        return ApiResponse<List<WaiterCustomerLookupResponse>>.Success(StatusCodes.Status200OK, response);
    }

    [HttpPost("waiter")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateForWaiter([FromBody] CreateReservationForWaiterRequest request, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var waiterId = User.GetUserId();
        var reservationEntity = await _reservationService.CreateForWaiterAsync(waiterId, request.ToCreateForWaiterDTO(), ct);

        if (reservationEntity is null)
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "Failed to create reservation.");

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status201Created, reservationEntity.ToResponse());
    }


    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateReservation(
        [FromBody] UpdateReservationRequest request,
        CancellationToken ct)
    {

        var actorUserId = User.GetUserId();
        var actorIsWaiter = User.IsWaiter();

        var updatedReservation = await _reservationService.UpdateReservationAsync(actorUserId, actorIsWaiter, request.ToUpdateDTO(), ct);

        if (updatedReservation == null)
        {
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "Failed to update reservation.");
        }

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, updatedReservation.ToResponse());
    }
}
