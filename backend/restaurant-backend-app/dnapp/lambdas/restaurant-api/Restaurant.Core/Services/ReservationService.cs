using System.Security.Cryptography;
using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

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
            ? await _repo.QueryByWaiterAsync(actorUserId, ct)
            : await _repo.QueryByCustomerAsync(actorUserId, ct);
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
        var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct);
        if (location is null) return ReservationErrors.LocationNotFound;

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        var validateResult = ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);
        if (validateResult.IsFailed) return validateResult;

        var start = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepo.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);
        if (schedule is null) return ReservationErrors.NoWaiterAssigned;

        var waiter = await _userRepository.GetByIdAsync(schedule.WaiterId, ct);
        if (waiter is null) return ReservationErrors.WaiterNotFound;

        var customer = await _userRepository.GetByIdAsync(customerId, ct);
        if (customer is null) return ReservationErrors.CustomerNotFound;

        var reservation = new Reservation
        {
            Id = Guid.NewGuid().ToString(),
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

        var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct);
        if (location is null) return ReservationErrors.LocationNotFound;

        var (startDate, endDate) = ReservationTimeHelper.ResolveReservationDates(dto.Date, dto.TimeFrom, dto.TimeTo, location);
        var validateResult = ReservationTimeHelper.ValidateReservationTime(dto.TimeFrom, dto.TimeTo, startDate, location);
        if (validateResult.IsFailed) return validateResult;

        var start = ReservationTimeHelper.ToDateTimeOffset(startDate, dto.TimeFrom, location.TimeZone);
        var end = ReservationTimeHelper.ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

        var slots = ReservationTimeHelper.GenerateSlots(start, end);

        var schedule = await _waiterScheduleRepo.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);
        if (schedule is null) return ReservationErrors.NoWaiterAssigned;

        if (!string.Equals(schedule.WaiterId, waiterId, StringComparison.Ordinal))
            return ReservationErrors.WaiterNotAssignedForCreation;

        var secretCode = Guid.NewGuid().ToString();
        
        var reservation = new Reservation
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            CustomerName = customerName,
            WaiterId = waiterId,
            WaiterName = $"{actor.FirstName} {actor.LastName}",
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

        var location = await _locationRepo.GetByIdAsync(reservation.LocationId, ct);
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
            var schedule = await _waiterScheduleRepo.GetAsync(
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

        reservation.Status = ReservationStatus.MealsServed;
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var success = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.InProgress,
            ReservationStatus.MealsServed,
            ct);

        if (!success)
            return ReservationErrors.MarkFailed;

        return reservation;
    }

    public async Task<Result<Reservation>> FinishReservationAsync(string reservationId, string waiterId, CancellationToken ct = default)
    {
        var getResult = await GetByIdAsync(reservationId, waiterId, actorIsWaiter: true, ct);
        if (getResult.IsFailed) return getResult;

        var reservation = getResult.Value;

        if (reservation.Status != ReservationStatus.MealsServed)
            return ReservationErrors.NotFinishable;

        var actualEnd = DateTimeOffset.UtcNow.ToString("O");
        reservation.Status = ReservationStatus.Finished;
        reservation.ActualEndTime = actualEnd;
        reservation.UpdatedAt = actualEnd;

        var slots = ReservationTimeHelper.GenerateSlots(
            DateTimeOffset.Parse(reservation.StartDateTime),
            DateTimeOffset.Parse(reservation.EndDateTime));

        var success = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.MealsServed,
            ReservationStatus.Finished,
            slots,
            ct);

        if (!success)
            return ReservationErrors.FinishFailed;

        return reservation;
    }
    
}
