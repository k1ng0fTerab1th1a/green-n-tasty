using FluentResults;
using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IOrderService
{
    Task<Result<Order>> CreateAsyncForReservation(string actorId, CreateOrderDTO dto, CancellationToken ct);

    Task<Result<Order>> GetByReservationAsync(string actorId, string reservationId, CancellationToken ct);

    Task<Result<Order>> AddDishAsync(string actorId, string reservationId, AddDishToOrderDTO dto, CancellationToken ct);

    Task<Result<Order>> DeleteDishAsync(string actorId, string reservationId, DeleteDishFromOrderDTO dto, CancellationToken ct);

    Task<Result<Order>> CompleteAsync(string actorId, string reservationId, CompleteOrderDTO dto, CancellationToken ct);
}