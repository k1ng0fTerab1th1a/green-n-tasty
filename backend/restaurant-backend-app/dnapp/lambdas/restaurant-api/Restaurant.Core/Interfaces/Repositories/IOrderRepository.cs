using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task CreateAsync(Order order, CancellationToken ct);
    Task<bool> CreateWithReservationUpdateAsync(
        Order order,
        string reservationId,
        string waiterId,
        int dishCount,
        string updatedAt,
        CancellationToken ct);
    Task<Order?> GetByReservationIdAsync(string reservationId, CancellationToken ct);
}