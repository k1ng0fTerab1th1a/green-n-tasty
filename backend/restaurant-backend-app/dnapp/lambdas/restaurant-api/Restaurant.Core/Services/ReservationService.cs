using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
namespace Restaurant.Core.Services;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository _repo;
    private readonly IWaiterScheduleRepository _waiterScheduleRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly ITableRepository _tableRepository;

    public ReservationService(IReservationRepository repo, IWaiterScheduleRepository waiterScheduleRepo, ILocationRepository locationRepo, ITableRepository tableRepository)
    {
        _repo = repo;
        _waiterScheduleRepo = waiterScheduleRepo;
        _locationRepo = locationRepo;
        _tableRepository = tableRepository;
    }

    public async Task<IReadOnlyList<Reservation>> GetMyAsync(string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return Array.Empty<Reservation>();

        return actorIsWaiter
            ? await _repo.QueryByWaiterAsync(actorUserId, ct: ct)
            : await _repo.QueryByCustomerAsync(actorUserId, ct: ct);
    }

    public async Task<Reservation?> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
    {
        var r = await _repo.GetByIdAsync(id, ct);
        if (r is null) return null;

        var allowed = r.CustomerId == actorUserId || (actorIsWaiter && r.WaiterId == actorUserId);
        if (!allowed) throw new UnauthorizedAccessException("Forbidden.");

        return r;
    }

    public async Task<bool> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct = default)
    {
        var reservation = await GetByIdAsync(reservationId, userId, isWaiter, ct)
            ?? throw new ArgumentNullException("reservation", "Reservation does not exist");

        if (reservation.Status != ReservationStatus.Reserved)
            throw new BusinessException("Only reserved reservations can be cancelled.");

        var start = DateTimeOffset.Parse(reservation.StartDateTime);
        var end = DateTimeOffset.Parse(reservation.EndDateTime);

        if ((start - DateTimeOffset.UtcNow).TotalMinutes < 30)
            throw new BusinessException("Reservation cannot be cancelled less than 30 minutes before it starts.");

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var success = await _repo.CancelReservationAsync(reservation, slots, ct);
        if (!success)
            throw new BusinessException("Failed to cancel reservation.");

        return true;
    }

    public async Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
    {
        var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct)
                    ?? throw new BusinessException("Location not found.");

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);

        var start = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepo.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);

        var waiterId = schedule?.WaiterId ?? throw new BusinessException("No waiter assigned for this table on this date.");

        var reservation = new Reservation
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            WaiterId = waiterId,
            LocationId = dto.LocationId,
            LocationAddress = location.Address,
            TableNumber = dto.TableNumber,
            TableKey = $"{dto.LocationId}#{dto.TableNumber}",
            StartDateTime = start.ToString("yyyy-MM-ddTHH:mmzzz"),
            EndDateTime = end.ToString("yyyy-MM-ddTHH:mmzzz"),
            GuestsCount = dto.GuestsCount,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };

        var success = await _repo.CreateWithSlotsAsync(reservation, dto.Date, slots, ct);

        if (!success)
            throw new SlotUnavailableException();

        return reservation;
    }

    public async Task<Reservation?> UpdateReservationAsync(string actorUserId, bool isActorWaiter,
        UpdateReservationDTO dto,
        CancellationToken ct = default)
    {
        var reservation = await GetByIdAsync(dto.Id, actorUserId, isActorWaiter, ct)
                          ?? throw new ArgumentNullException("reservation", "Reservation does not exist");

        if (reservation.Status != ReservationStatus.Reserved)
            throw new BusinessException("Only reserved reservations can be updated.");

        var location = await _locationRepo.GetByIdAsync(reservation.LocationId, ct)
                       ?? throw new BusinessException("Location not found.");

        var oldStart = DateTimeOffset.Parse(reservation.StartDateTime);
        var oldEnd = DateTimeOffset.Parse(reservation.EndDateTime);

        if ((oldStart - DateTimeOffset.UtcNow).TotalMinutes < 30)
            throw new BusinessException("Reservation cannot be updated less than 30 minutes before it starts.");

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);

        var newStart = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var newEnd = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        List<string> oldSlots = ReservationTimeHelper.GenerateSlots(oldStart, oldEnd);
        List<string> newSlots = ReservationTimeHelper.GenerateSlots(newStart, newEnd);

        var oldTableKey = reservation.TableKey;

        var table = await _tableRepository.GetByLocationAndTableNumberAsync(
            reservation.LocationId, dto.TableNumber, ct)
            ?? throw new BusinessException("Table not found.");

        if (table.Capacity < dto.GuestNumber)
            throw new BusinessException("Amount of guests exceeds the table capacity.");

        reservation.GuestsCount = dto.GuestNumber;
        reservation.StartDateTime = newStart.ToString("yyyy-MM-ddTHH:mmzzz");
        reservation.EndDateTime = newEnd.ToString("yyyy-MM-ddTHH:mmzzz");
        reservation.UpdatedAt = DateTime.UtcNow.ToString("O");

        reservation.TableNumber = dto.TableNumber;
        reservation.TableKey = $"{reservation.LocationId}#{dto.TableNumber}";

        return await _repo.UpdateReservationAsync(reservation, newSlots, oldSlots, oldTableKey, oldStart, ct);
    }
}
