using System.Globalization;
using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using TimeZoneConverter;

namespace Restaurant.Core.Services
{
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

            var slots = GenerateSlots(start, end);

            var success = await _repo.CancelReservationAsync(reservation, slots, ct);
            if (!success)
                throw new BusinessException("Failed to cancel reservation.");

            return true;
        }
        
        public async Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
        {
            var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct)
                        ?? throw new BusinessException("Location not found.");

            ValidateReservationTime(dto.TimeFrom, dto.TimeTo, dto.Date, location);

            var endDate = dto.TimeTo < dto.TimeFrom ? dto.Date.AddDays(1) : dto.Date;
            var start = ToDateTimeOffset(dto.Date, dto.TimeFrom, location.TimeZone);
            var end = ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);

            var slots = GenerateSlots(start, end);

            var schedule = await _waiterScheduleRepo.GetAsync($"{dto.LocationId}#{dto.TableNumber}", dto.Date.ToString("yyyy-MM-dd"), ct);

            var waiterId = schedule?.WaiterId ?? throw new BusinessException("No waiter assigned for this table on this date.");

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
            var oldEnd   = DateTimeOffset.Parse(reservation.EndDateTime);

            if ((oldStart - DateTimeOffset.UtcNow).TotalMinutes < 30)
                throw new BusinessException("Reservation cannot be updated less than 30 minutes before it starts.");
            
            ValidateReservationTime(dto.TimeFrom, dto.TimeTo, dto.Date, location);

            var endDate = dto.TimeTo < dto.TimeFrom ? dto.Date.AddDays(1) : dto.Date;
            var newStart = ToDateTimeOffset(dto.Date, dto.TimeFrom, location.TimeZone);
            var newEnd = ToDateTimeOffset(endDate, dto.TimeTo, location.TimeZone);
            

            List<string> oldSlots = GenerateSlots(oldStart, oldEnd);
            List<string> newSlots = GenerateSlots(newStart, newEnd);

            bool isTableDifferent = reservation.TableNumber != dto.TableNumber;
            bool isDayDifferent   = newStart.Date != oldStart.Date;

            Table? table = await _tableRepository.GetByLocationAndTableNumberAsync(
                reservation.LocationId,
                isTableDifferent ? dto.TableNumber : reservation.TableNumber,
                ct) ?? throw new ArgumentNullException("dto", "This table does not exist");

            if (table.Capacity < dto.GuestNumber)
                throw new BusinessException("Amount of guests exceeds the table capacity");

            var oldTableKey   = reservation.TableKey; 
            var oldDateString = reservation.StartDateTime;
            // Apply new values to reservation object before passing to repo
            reservation.GuestsCount   = dto.GuestNumber;
            reservation.StartDateTime = newStart.ToString("yyyy-MM-ddTHH:mmzzz");
            reservation.EndDateTime   = newEnd.ToString("yyyy-MM-ddTHH:mmzzz");
            reservation.UpdatedAt     = DateTime.UtcNow.ToString("O");

            if (isTableDifferent)
            {
                reservation.TableNumber = dto.TableNumber;
                reservation.TableKey    = $"{reservation.LocationId}#{dto.TableNumber}";
            }

            return await _repo.UpdateReservationAsync(reservation, newSlots, oldSlots, oldTableKey, 
                oldDateString, 
                isDayDifferent,
                isTableDifferent ? table : null, ct);
        }

        private static void ValidateReservationTime(TimeOnly from, TimeOnly to, DateOnly date, Location location)
        {
            var openTime = TimeOnly.Parse(location.OpenTime);
            var closeTime = TimeOnly.Parse(location.CloseTime);
            var crossMidnight = closeTime < openTime;

            var fromValid = crossMidnight
                ? from >= openTime || from < closeTime
                : from >= openTime && from < closeTime;

            var toValid = crossMidnight
                ? to > openTime || to <= closeTime
                : to > openTime && to <= closeTime;

            if (!fromValid || !toValid)
                throw new BusinessException($"Reservation must be within working hours ({location.OpenTime} - {location.CloseTime}).");

            if (from.Minute % 15 != 0 || from.Second != 0)
                throw new BusinessException("Start time must be a multiple of 15 minutes.");

            if (to.Minute % 15 != 0 || to.Second != 0)
                throw new BusinessException("End time must be a multiple of 15 minutes.");

            var duration = CalculateDuration(from, to);
            if (duration < 60)
                throw new BusinessException("Minimum booking duration is 60 minutes.");

            if (duration > 6 * 60)
                throw new BusinessException("Maximum booking duration is 6 hours.");

            if (date.ToDateTime(from) <= DateTime.UtcNow)
                throw new BusinessException("Cannot book for a past date or time.");
        }
        private static int CalculateDuration(TimeOnly from, TimeOnly to)
        {
            if (to > from)
                return (int)(to - from).TotalMinutes;

            var toMidnight = (int)(TimeOnly.MaxValue - from).TotalMinutes + 1;
            var fromMidnight = to.Hour * 60 + to.Minute;
            return toMidnight + fromMidnight;
        }
        private static List<string> GenerateSlots(DateTimeOffset start, DateTimeOffset end)
        {
            var slots = new List<string>();
            var cursor = start;

            while (true)
            {
                slots.Add(cursor.ToString("yyyy-MM-ddTHH:mmzzz"));
                if (cursor == end) break;
                cursor = cursor.AddMinutes(15);
            }

            return slots;
        }
        private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time, string timeZoneId)
        {
            var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
            var dt = date.ToDateTime(time);
            return new DateTimeOffset(dt, tz.GetUtcOffset(dt));
        }
    }
}
