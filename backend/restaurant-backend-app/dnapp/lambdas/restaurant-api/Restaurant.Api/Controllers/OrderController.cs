using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrderController (IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "WAITER")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status201Created)]
    public async Task<ApiResponse<OrderResponse>> CreateOrderForReservation(
        [FromBody] CreateOrderDTO request,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.CreateAsyncForReservation(actorUserId, request, ct);

        if(result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status201Created, result.Value.ToResponse());
    }

    [HttpGet("reservations/{reservationId}")]
    [Authorize(Roles = "WAITER")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<OrderResponse>> GetOrderByReservation(
        [FromRoute] string reservationId,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.GetByReservationAsync(actorUserId, reservationId, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("reservations/{reservationId}/dishes")]
    [Authorize(Roles = "WAITER")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<OrderResponse>> AddDish(
        [FromRoute] string reservationId,
        [FromBody] AddDishToOrderDTO request,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.AddDishAsync(actorUserId, reservationId, request, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("reservations/{reservationId}/dishes/remove")]
    [Authorize(Roles = "WAITER")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<OrderResponse>> DeleteDish(
        [FromRoute] string reservationId,
        [FromBody] DeleteDishFromOrderDTO request,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.DeleteDishAsync(actorUserId, reservationId, request, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpPost("reservations/{reservationId}/complete")]
    [Authorize(Roles = "WAITER")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<OrderResponse>> CompleteOrder(
        [FromRoute] string reservationId,
        [FromBody] CompleteOrderDTO request,
        CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.CompleteAsync(actorUserId, reservationId, request, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }
}
