using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using TimeZoneConverter;

namespace Restaurant.Core.Services;

public sealed class ReservationService : IReservationService
{
    private const string WaiterRole = "WAITER";
    private const string CustomerRole = "CUSTOMER";

    private readonly IReservationRepository _repo;
    private readonly IWaiterScheduleRepository _waiterScheduleRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly ITableRepository _tableRepository;
    private readonly IUserRepository _userRepository;

    public ReservationService(
        IReservationRepository repo,
        IWaiterScheduleRepository waiterScheduleRepo,
        ILocationRepository locationRepo,
        ITableRepository tableRepository,
        IUserRepository userRepository)
    {
        _repo = repo;
        _waiterScheduleRepo = waiterScheduleRepo;
        _locationRepo = locationRepo;
        _tableRepository = tableRepository;
        _userRepository = userRepository;
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
            IsCreatedByWaiter = false,
            VisitorName = null,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };

        var success = await _repo.CreateWithSlotsAsync(reservation, dto.Date, slots, ct);

        if (!success)
            throw new SlotUnavailableException();

        return reservation;
    }

    public async Task<Reservation> CreateForWaiterAsync(string waiterId, CreateReservationForWaiterDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(waiterId))
            throw new UnauthorizedAccessException("Forbidden.");

        var actor = await _userRepository.GetByIdAsync(waiterId, ct)
                    ?? throw new UnauthorizedAccessException("Forbidden.");

        if (!string.Equals(actor.Role, WaiterRole, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Forbidden.");

        var customerId = string.IsNullOrWhiteSpace(dto.CustomerId) ? null : dto.CustomerId.Trim();
        var visitorName = string.IsNullOrWhiteSpace(dto.VisitorName) ? null : dto.VisitorName.Trim();

        if ((customerId is null && visitorName is null) || (customerId is not null && visitorName is not null))
            throw new BusinessException("Exactly one of customerId or visitorName must be provided.");

        if (customerId is not null)
        {
            var customer = await _userRepository.GetByIdAsync(customerId, ct)
                           ?? throw new BusinessException("Customer not found.");

            if (!string.Equals(customer.Role, CustomerRole, StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("Customer not found.");
        }

        var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct)
                    ?? throw new BusinessException("Location not found.");

        ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, dto.Date, location);

        var endDate = dto.TimeTo < dto.TimeFrom ? dto.Date.AddDays(1) : dto.Date;
        var start = ReservationTimeHelper.ToDateTimeOffset(dto.Date, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepo.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct)
                       ?? throw new BusinessException("No waiter assigned for this table on this date.");

        if (!string.Equals(schedule.WaiterId, waiterId, StringComparison.Ordinal))
            throw new BusinessException("Waiter can create reservations only for assigned tables.");

        var reservation = new Reservation
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            WaiterId = waiterId,
            LocationId = dto.LocationId,
            TableNumber = dto.TableNumber,
            TableKey = $"{dto.LocationId}#{dto.TableNumber}",
            StartDateTime = start.ToString("yyyy-MM-ddTHH:mmzzz"),
            EndDateTime = end.ToString("yyyy-MM-ddTHH:mmzzz"),
            GuestsCount = dto.GuestsCount,
            Status = ReservationStatus.Reserved,
            IsCreatedByWaiter = true,
            VisitorName = visitorName,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };

        var success = await _repo.CreateWithSlotsAsync(reservation, dto.Date, slots, ct);

        if (!success)
            throw new SlotUnavailableException();

        return reservation;
    }

    public async Task<IReadOnlyList<WaiterCustomerLookupDTO>> SearchCustomersForWaiterAsync(
        string actorUserId,
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            throw new UnauthorizedAccessException("Forbidden.");

        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<WaiterCustomerLookupDTO>();

        var actor = await _userRepository.GetByIdAsync(actorUserId, ct)
                    ?? throw new UnauthorizedAccessException("Forbidden.");

        if (!string.Equals(actor.Role, WaiterRole, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Forbidden.");

        var customers = await _userRepository.SearchCustomersAsync(query, ct);

        return customers
            .Select(x => new WaiterCustomerLookupDTO(
                x.UserId,
                $"{x.FirstName} {x.LastName}".Trim(),
                EmailMaskingHelper.Mask(x.Email)))
            .ToList();
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
