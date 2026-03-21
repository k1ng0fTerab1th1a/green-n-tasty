using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> QueryByCustomerAsync(
        string customerId,
        string? startFromIso = null,
        string? startToIso = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> QueryByWaiterAsync(
        string waiterId,
        string? startFromIso = null,
        string? startToIso = null,
        CancellationToken ct = default);

    Task<bool> CreateWithSlotsAsync(Reservation reservation, DateOnly date, List<string> slots, CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> QueryByTableAsync(
        string tableKey,
        string startFromIso,
        string startToIso,
        CancellationToken ct);

    Task<bool> CancelReservationAsync(Reservation reservation, List<string> slots,
        CancellationToken ct = default);

    Task<Reservation?> UpdateReservationAsync(
        Reservation reservation,
        List<string> newSlots,
        List<string> oldSlots,
        string oldTableKey,
        DateTimeOffset oldStart,
        CancellationToken ct = default);

    Task<bool> UpdateLifecycleAsync(
        Reservation reservation,
        ReservationStatus expectedCurrentStatus,
        ReservationStatus newStatus,
        List<string>? slotsToRelease = null,
        CancellationToken ct = default);
}
