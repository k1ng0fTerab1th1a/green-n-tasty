using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task CreateAsync(Order order, CancellationToken ct = default);
    Task<bool> CreateWithReservationUpdateAsync(
        Order order,
        string reservationId,
        string waiterId,
        int dishCount,
        string updatedAt,
        CancellationToken ct = default);
}