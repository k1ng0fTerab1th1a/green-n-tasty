using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task CreateAsync(Order order, CancellationToken ct);

    Task<Order?> GetByReservationIdAsync(string reservationId, CancellationToken ct);

    Task<bool> CreateWithReservationUpdateAsync(
        Order order,
        string reservationId,
        string waiterId,
        int dishCount,
        string updatedAt,
        CancellationToken ct);

    Task<bool> UpdateWithReservationDishCountAsync(
        Order order,
        int expectedOrderVersion,
        string operationId,
        int dishCountDelta,
        string updatedAt,
        CancellationToken ct);

    Task<bool> CompleteAsync(
        Order order,
        string waiterId,
        int expectedOrderVersion,
        string operationId,
        CancellationToken ct);
}
