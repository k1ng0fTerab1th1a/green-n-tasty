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
[Authorize]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReservationResponse>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<ReservationResponse>>> GetMy(CancellationToken ct)
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<ReservationResponse>> GetById([FromRoute] string id, CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var actorIsWaiter = User.IsWaiter();

        var result = await _reservationService.GetByIdAsync(id, actorUserId, actorIsWaiter, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<object>> Delete(string id, CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var actorIsWaiter = User.IsWaiter();

        var result = await _reservationService.CancelReservation(id, actorUserId, actorIsWaiter, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Reservation cancelled successfully.");
    }

    [HttpPost("client")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ApiResponse<ReservationResponse>> CreateForClient([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var customerId = User.GetUserId();
        var result = await _reservationService.CreateForClientAsync(customerId, request.ToCreateDTO(), ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status201Created, result.Value.ToResponse());
    }

    [HttpGet("waiter/customers")]
    [ProducesResponseType(typeof(ApiResponse<List<WaiterCustomerLookupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<List<WaiterCustomerLookupResponse>>> SearchCustomersForWaiter([FromQuery] string query, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<List<WaiterCustomerLookupResponse>>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var actorUserId = User.GetUserId();
        var result = await _reservationService.SearchCustomersForWaiterAsync(actorUserId, query, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<WaiterCustomerLookupResponse>>();

        var response = result.Value
            .Select(x => x.ToWaiterCustomerLookupResponse())
            .ToList();

        return ApiResponse<List<WaiterCustomerLookupResponse>>.Success(StatusCodes.Status200OK, response);
    }

    [HttpPost("waiter")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<ReservationResponse>> CreateForWaiter([FromBody] CreateReservationForWaiterRequest request, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<ReservationResponse>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var waiterId = User.GetUserId();
        var result = await _reservationService.CreateForWaiterAsync(waiterId, request.ToCreateForWaiterDTO(), ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status201Created, result.Value.ToResponse());
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ApiResponse<ReservationResponse>> UpdateReservation(
        [FromBody] UpdateReservationRequest request,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var actorIsWaiter = User.IsWaiter();

        var result = await _reservationService.UpdateReservationAsync(actorUserId, actorIsWaiter, request.ToUpdateDTO(), ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ReservationResponse>> StartReservation([FromRoute] string id, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<ReservationResponse>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var waiterId = User.GetUserId();
        var result = await _reservationService.StartReservationAsync(id, waiterId, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("{id}/meals-served")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ReservationResponse>> MarkMealsServed([FromRoute] string id, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<ReservationResponse>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var waiterId = User.GetUserId();
        var result = await _reservationService.MarkMealsServedAsync(id, waiterId, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("{id}/finish")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ReservationResponse>> FinishReservation([FromRoute] string id, CancellationToken ct)
    {
        if (!User.IsWaiter())
            return ApiResponse<ReservationResponse>.Fail(StatusCodes.Status403Forbidden, "Forbidden.");

        var waiterId = User.GetUserId();
        var result = await _reservationService.FinishReservationAsync(id, waiterId, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<ReservationResponse>();

        return ApiResponse<ReservationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }
}
