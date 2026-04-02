using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Messaging;
using Restaurant.Core.Models;
using System.Text.Json;

namespace Restaurant.Core.Services;

public sealed class ReservationService : IReservationService
{
    private const string WaiterRole = "WAITER";
    private const string CustomerRole = "CUSTOMER";

    private readonly IReservationRepository _repo;
    private readonly IWaiterScheduleRepository _waiterScheduleRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDishRepository _dishRepository;
    private readonly IEventPublisher _eventPublisher;

    public ReservationService(
        IReservationRepository repo,
        IWaiterScheduleRepository waiterScheduleRepository,
        ILocationRepository locationRepository,
        ITableRepository tableRepository,
        IUserRepository userRepository,
        IDishRepository dishRepository,
        IEventPublisher eventPublisher)
    {
        _repo = repo;
        _waiterScheduleRepository = waiterScheduleRepository;
        _locationRepository = locationRepository;
        _tableRepository = tableRepository;
        _userRepository = userRepository;
        _dishRepository = dishRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<IReadOnlyList<Reservation>>> GetByCustomer(string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return Result.Ok<IReadOnlyList<Reservation>>(Array.Empty<Reservation>());

        var reservations = await _repo.QueryByCustomerAsync(actorUserId, ct);
        return Result.Ok(reservations);
    }

    public async Task<Result<IReadOnlyList<Reservation>>> GetByWaiter(string actorUserId, DateOnly? date, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return Result.Ok<IReadOnlyList<Reservation>>(Array.Empty<Reservation>());

        IReadOnlyList<Reservation> reservations;
        if (date.HasValue)
        {
            var startPrefix = date.Value.ToString("yyyy-MM-dd");
            reservations = await _repo.QueryByWaiterAsync(actorUserId, startPrefix, ct);
        }
        else
        {
            reservations = await _repo.QueryByWaiterAsync(actorUserId, ct);
        }

        return Result.Ok(reservations);
    }

    public async Task<Result<Reservation>> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
    {
        var r = await _repo.GetByIdAsync(id, ct);
        if (r is null) return ReservationErrors.ReservationNotFound;

        var allowed = r.CustomerId == actorUserId || (actorIsWaiter && r.WaiterId == actorUserId);
        if (!allowed) return ReservationErrors.Forbidden;

        return r;
    }
    
    public async Task<Result> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(reservationId, userId, isWaiter, ct);
        if (getResult.IsFailed) return getResult.ToResult();

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.Reserved)
            return ReservationErrors.NotCancellable;

        var start = DateTimeOffset.Parse(reservation.StartDateTime);
        var end = DateTimeOffset.Parse(reservation.EndDateTime);

        if ((start - DateTimeOffset.UtcNow).TotalMinutes < 30)
            return ReservationErrors.TooLateToCancel;

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var success = await _repo.CancelReservationAsync(reservation, slots, ct);
        if (!success)
            return ReservationErrors.CancellationFailed;

        return Result.Ok();
    }

    public async Task<Result<Reservation>> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
    {
        var location = await _locationRepository.GetByIdAsync(dto.LocationId, ct);
        if (location is null) return ReservationErrors.LocationNotFound;

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        var validateResult = ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);
        if (validateResult.IsFailed) return validateResult;

        var start = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepository.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);
        if (schedule is null) return ReservationErrors.NoWaiterAssigned;

        var waiter = await _userRepository.GetByIdAsync(schedule.WaiterId, ct);
        if (waiter is null) return ReservationErrors.WaiterNotFound;

        var customer = await _userRepository.GetByIdAsync(customerId, ct);
        if (customer is null) return ReservationErrors.CustomerNotFound;

        var reservation = new Reservation
        {
            Id = GenerateReservationId(location.Address, dto.TableNumber, start),
            CustomerId = customerId,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            WaiterId = schedule.WaiterId,
            WaiterName = $"{waiter.FirstName} {waiter.LastName}",
            LocationId = dto.LocationId,
            LocationAddress = location.Address,
            TableNumber = dto.TableNumber,
            TableKey = $"{dto.LocationId}#{dto.TableNumber}",
            StartDateTime = start.ToString("yyyy-MM-ddTHH:mmzzz"),
            EndDateTime = end.ToString("yyyy-MM-ddTHH:mmzzz"),
            ActualStartTime = null,
            ActualEndTime = null,
            GuestsCount = dto.GuestsCount,
            Status = ReservationStatus.Reserved,
            IsCreatedByWaiter = false,
            VisitorName = null,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
            SecretCode = null
        };

        var success = await _repo.CreateWithSlotsAsync(reservation, dto.Date, slots, ct);
        if (!success) return ReservationErrors.SlotUnavailable;

        return reservation;
    }

    public async Task<Result<Reservation>> CreateForWaiterAsync(string waiterId, CreateReservationForWaiterDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(waiterId))
            return ReservationErrors.Forbidden;

        var actor = await _userRepository.GetByIdAsync(waiterId, ct);
        if (actor is null) return ReservationErrors.Forbidden;

        if (!string.Equals(actor.Role, WaiterRole, StringComparison.OrdinalIgnoreCase))
            return ReservationErrors.Forbidden;

        var waiterLocationId = actor.LocationId?.Trim();
        if (string.IsNullOrWhiteSpace(waiterLocationId))
            return ReservationErrors.WaiterLocationNotConfigured;

        var customerId = string.IsNullOrWhiteSpace(dto.CustomerId) ? null : dto.CustomerId.Trim();
        var visitorName = string.IsNullOrWhiteSpace(dto.VisitorName) ? null : dto.VisitorName.Trim();

        if ((customerId is null && visitorName is null) || (customerId is not null && visitorName is not null))
            return ReservationErrors.CustomerOrVisitorRequired;

        string? customerName = null;
        if (customerId is not null)
        {
            var customer = await _userRepository.GetByIdAsync(customerId, ct);
            if (customer is null) return ReservationErrors.CustomerNotFound;

            if (!string.Equals(customer.Role, CustomerRole, StringComparison.OrdinalIgnoreCase))
                return ReservationErrors.CustomerNotFound;

            customerName = $"{customer.FirstName} {customer.LastName}";
        }

        var location = await _locationRepository.GetByIdAsync(waiterLocationId, ct);
        if (location is null) return ReservationErrors.LocationNotFound;

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        var validateResult = ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);
        if (validateResult.IsFailed) return validateResult;

        var start = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepository.GetAsync($"{waiterLocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);
        if (schedule is null) return ReservationErrors.NoWaiterAssigned;

        if (!string.Equals(schedule.WaiterId, waiterId, StringComparison.Ordinal))
            return ReservationErrors.WaiterNotAssignedForCreation;

        var secretCode = Guid.NewGuid().ToString();
        
        var reservation = new Reservation
        {
            Id = GenerateReservationId(location.Address, dto.TableNumber, start),
            CustomerId = customerId,
            CustomerName = customerName,
            WaiterId = waiterId,
            WaiterName = $"{actor.FirstName} {actor.LastName}",
            LocationId = waiterLocationId,
            LocationAddress = location.Address,
            TableNumber = dto.TableNumber,
            TableKey = $"{waiterLocationId}#{dto.TableNumber}",
            StartDateTime = start.ToString("yyyy-MM-ddTHH:mmzzz"),
            EndDateTime = end.ToString("yyyy-MM-ddTHH:mmzzz"),
            ActualStartTime = null,
            ActualEndTime = null,
            GuestsCount = dto.GuestsCount,
            Status = ReservationStatus.Reserved,
            IsCreatedByWaiter = true,
            VisitorName = visitorName,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
            SecretCode = secretCode,
        };

        var success = await _repo.CreateWithSlotsAsync(reservation, dto.Date, slots, ct);
        if (!success) return ReservationErrors.SlotUnavailable;

        return reservation;
    }

    public async Task<Result<IReadOnlyList<WaiterCustomerLookupDTO>>> SearchCustomersForWaiterAsync(
        string actorUserId,
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return ReservationErrors.Forbidden;

        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<WaiterCustomerLookupDTO>();

        var actor = await _userRepository.GetByIdAsync(actorUserId, ct);
        if (actor is null) return ReservationErrors.Forbidden;

        if (!string.Equals(actor.Role, WaiterRole, StringComparison.OrdinalIgnoreCase))
            return ReservationErrors.Forbidden;

        var customers = await _userRepository.SearchCustomersAsync(query, ct);

        var result = customers
            .Select(x => new WaiterCustomerLookupDTO(
                x.UserId,
                $"{x.FirstName} {x.LastName}".Trim(),
                EmailMaskingHelper.Mask(x.Email)))
            .ToList();

        return result;
    }

    public async Task<Result<Reservation>> UpdateReservationAsync(string actorUserId, bool isActorWaiter,
        UpdateReservationDTO dto,
        CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(dto.Id, actorUserId, isActorWaiter, ct);
        if (getResult.IsFailed) return getResult;

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.Reserved)
            return ReservationErrors.NotUpdatable;

        var location = await _locationRepository.GetByIdAsync(reservation.LocationId, ct);
        if (location is null) return ReservationErrors.LocationNotFound;

        var oldStart = DateTimeOffset.Parse(reservation.StartDateTime);
        var oldEnd = DateTimeOffset.Parse(reservation.EndDateTime);

        if ((oldStart - DateTimeOffset.UtcNow).TotalMinutes < 30)
            return ReservationErrors.TooLateToUpdate;

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        var validateResult = ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);
        if (validateResult.IsFailed) return validateResult;

        var newStart = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var newEnd = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        List<string> oldSlots = ReservationTimeHelper.GenerateSlots(oldStart, oldEnd);
        List<string> newSlots = ReservationTimeHelper.GenerateSlots(newStart, newEnd);

        var oldTableKey = reservation.TableKey;
        var oldTableNumber = reservation.TableNumber;

        var table = await _tableRepository.GetByLocationAndTableNumberAsync(reservation.LocationId, dto.TableNumber, ct);
        if (table is null) return ReservationErrors.TableNotFound;

        if (table.Capacity < dto.GuestNumber)
            return ReservationErrors.TableCapacityExceeded;

        var reservationDateChanged = oldStart.ToString("yyyy-MM-dd") != dto.Date.ToString("yyyy-MM-dd");
        var reservationTimeChanged = oldStart != newStart || oldEnd != newEnd;
        var reservationTableChanged = oldTableNumber != dto.TableNumber;

        if (isActorWaiter && (reservationDateChanged || reservationTimeChanged || reservationTableChanged))
        {
            var schedule = await _waiterScheduleRepository.GetAsync(
                $"{reservation.LocationId}#{dto.TableNumber}",
                dto.Date.ToString("yyyy-MM-dd"),
                ct);

            if (schedule is null) return ReservationErrors.NoWaiterAssigned;

            if (!string.Equals(schedule.WaiterId, actorUserId, StringComparison.Ordinal))
                return ReservationErrors.WaiterNotAssignedForUpdate;
        }

        reservation.GuestsCount = dto.GuestNumber;
        reservation.StartDateTime = newStart.ToString("yyyy-MM-ddTHH:mmzzz");
        reservation.EndDateTime = newEnd.ToString("yyyy-MM-ddTHH:mmzzz");
        reservation.UpdatedAt = DateTime.UtcNow.ToString("O");
        reservation.TableNumber = dto.TableNumber;
        reservation.TableKey = $"{reservation.LocationId}#{dto.TableNumber}";

        return await _repo.UpdateReservationAsync(reservation, newSlots, oldSlots, oldTableKey, oldStart, ct);
    }

    public async Task<Result<Reservation>> StartReservationAsync(string reservationId, string waiterId, CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(reservationId, waiterId, actorIsWaiter: true, ct);
        if (getResult.IsFailed) return getResult;

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.Reserved)
            return ReservationErrors.NotStartable;

        var now = DateTimeOffset.UtcNow.ToString("O");
        reservation.Status = ReservationStatus.InProgress;
        reservation.ActualStartTime ??= now;
        reservation.UpdatedAt = now;

        var success = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.Reserved,
            ReservationStatus.InProgress,
            ct);

        if (!success)
            return ReservationErrors.StartFailed;

        return reservation;
    }

    public async Task<Result<Reservation>> MarkMealsServedAsync(string reservationId, string waiterId, CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(reservationId, waiterId, actorIsWaiter: true, ct);
        if (getResult.IsFailed) return getResult;

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.InProgress)
            return ReservationErrors.NotMarkable;

        if (reservation.IsMealServed)
            return reservation;

        reservation.IsMealServed = true;
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await _repo.UpdateAsync(reservation, ct);

        return reservation;
    }

    public async Task<Result<Reservation>> FinishReservationAsync(
        string reservationId,
        string waiterId,
        CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(reservationId, waiterId, actorIsWaiter: true, ct);
        if (getResult.IsFailed)
            return getResult;

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.InProgress)
            return ReservationErrors.NotFinishable;

        var actualEnd = DateTimeOffset.UtcNow.ToString("O");
        reservation.Status = ReservationStatus.Finished;
        reservation.ActualEndTime = actualEnd;
        reservation.UpdatedAt = actualEnd;

        var slots = ReservationTimeHelper.GenerateSlots(
            DateTimeOffset.Parse(reservation.StartDateTime),
            DateTimeOffset.Parse(reservation.EndDateTime));

        var finishOutcome = await _repo.FinishAndCompleteOrderIfOpenAsync(
            reservation,
            slots,
            ct);

        if (!finishOutcome.IsSuccess)
            return ReservationErrors.FinishFailed;

        if (finishOutcome.OrderWasCompleted)
        {
            foreach (var increment in finishOutcome.PopularityIncrements)
            {
                if (string.IsNullOrWhiteSpace(increment.DishId) || increment.Quantity <= 0)
                    continue;

                await _dishRepository.IncrementPopularityAsync(
                    increment.DishId,
                    increment.Quantity,
                    ct);
            }
        }

        var reservationCompletedEvent = new SqsEvent(
            EventTypes.ReservationCompleted,
            JsonSerializer.SerializeToElement(new ReservationCompletedDTO(
                reservation.Id,
                DateTimeOffset.Parse(actualEnd))));

        await _eventPublisher.PublishAsync(reservationCompletedEvent, ct);

        return reservation;
    }
    
    private static string GenerateReservationId(string locationAddress, int tableNumber, DateTimeOffset start)
    {
        var addressCode = new string(
            locationAddress.Where(char.IsLetterOrDigit)
                .Take(6)
                .Select(char.ToUpperInvariant)
                .ToArray());

        var dateCode = start.ToString("ddMMyy");
        var timeCode = start.ToString("HHmm");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        return $"{addressCode}-{tableNumber}-{dateCode}-{timeCode}-{suffix}";
    }
}
