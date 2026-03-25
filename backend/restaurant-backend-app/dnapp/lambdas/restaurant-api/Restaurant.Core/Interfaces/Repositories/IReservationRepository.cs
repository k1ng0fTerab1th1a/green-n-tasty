using FluentResults;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(string id, CancellationToken ct);
    Task UpdateAsync(Reservation reservation, CancellationToken ct);

    Task<IReadOnlyList<Reservation>> QueryByCustomerAsync(string customerId, CancellationToken ct);

    Task<IReadOnlyList<Reservation>> QueryByCustomerAsync(
        string customerId,
        string? startFromIso,
        string? startToIso,
        CancellationToken ct);

    Task<IReadOnlyList<Reservation>> QueryByWaiterAsync(string waiterId, CancellationToken ct);

    Task<IReadOnlyList<Reservation>> QueryByWaiterAsync(
        string waiterId,
        string? startFromIso,
        string? startToIso,
        CancellationToken ct);

    Task<bool> CreateWithSlotsAsync(Reservation reservation, DateOnly date, List<string> slots, CancellationToken ct);

    Task<IReadOnlyList<Reservation>> QueryByTableAsync(
        string tableKey,
        string startFromIso,
        string startToIso,
        CancellationToken ct);

    Task<bool> CancelReservationAsync(Reservation reservation, List<string> slots,
        CancellationToken ct);

    Task<Result<Reservation>> UpdateReservationAsync(
        Reservation reservation,
        List<string> newSlots,
        List<string> oldSlots,
        string oldTableKey,
        DateTimeOffset oldStart,
        CancellationToken ct);

    Task<bool> UpdateLifecycleAsync(
        Reservation reservation,
        ReservationStatus expectedCurrentStatus,
        ReservationStatus newStatus,
        CancellationToken ct);

    Task<bool> UpdateLifecycleAsync(
        Reservation reservation,
        ReservationStatus expectedCurrentStatus,
        ReservationStatus newStatus,
        List<string> slotsToRelease,
        CancellationToken ct);
}
