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
    public async Task<ApiResponse<OrderResponse>> CreateOrderForReservation([FromBody] CreateOrderDTO request, CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        var result = await orderService.CreateAsyncForReservation(actorUserId, request, ct);

        if(result.IsFailed)
            return result.Errors[0].ToApiResponse<OrderResponse>();

        return ApiResponse<OrderResponse>.Success(StatusCodes.Status201Created, result.Value.ToResponse());
    }
}
