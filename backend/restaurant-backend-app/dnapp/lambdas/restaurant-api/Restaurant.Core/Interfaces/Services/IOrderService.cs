using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IOrderService
{
    Task<Result<Order>> CreateAsyncForReservation(string actorId, CreateOrderDTO dto, CancellationToken ct);
}