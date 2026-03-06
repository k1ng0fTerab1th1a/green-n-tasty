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

        public ReservationService(IReservationRepository repo, IWaiterScheduleRepository waiterScheduleRepo, ILocationRepository locationRepo)
        {
            _repo = repo;
            _waiterScheduleRepo = waiterScheduleRepo;
            _locationRepo = locationRepo;
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

        public async Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
        {
            var location = await _locationRepo.GetByIdAsync(dto.LocationId, ct)
                        ?? throw new BusinessException("Location not found.");

            ValidateReservationTime(dto.TimeFrom, dto.TimeTo, dto.Date, location);

            var slots = GenerateSlots(dto.Date, dto.TimeFrom, dto.TimeTo, location.TimeZone);

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
                StartDateTime = FormatWithOffset(dto.Date, dto.TimeFrom, location.TimeZone),
                EndDateTime = FormatWithOffset(dto.Date, dto.TimeTo, location.TimeZone),
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

            if (from == to)
                throw new BusinessException("Start and end times cannot be the same.");

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
        private static List<string> GenerateSlots(DateOnly date, TimeOnly from, TimeOnly to, string timeZoneId)
        {
            var slots = new List<string>();
            var cursor = from;
            var cursorDate = date;
            while (true)
            {
                slots.Add(FormatWithOffset(cursorDate, cursor, timeZoneId));
                if (cursor == to) break;
                cursor = cursor.AddMinutes(15);
                if (cursor == TimeOnly.MinValue)
                    cursorDate = cursorDate.AddDays(1);
            }
            return slots;
        }
        private static string FormatWithOffset(DateOnly date, TimeOnly time, string timeZoneId)
        {
            var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
            var dt = date.ToDateTime(time);
            var offset = tz.GetUtcOffset(dt);
            var dto = new DateTimeOffset(dt, offset);
            return dto.ToString("yyyy-MM-ddTHH:mmzzz");
        }
    }
}
