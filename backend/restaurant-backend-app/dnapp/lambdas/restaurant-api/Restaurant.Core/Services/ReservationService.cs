using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services
{
    public sealed class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repo;
        private readonly IWaiterScheduleRepository _waiterScheduleRepo;

        public ReservationService(IReservationRepository repo, IWaiterScheduleRepository waiterScheduleRepo)
        {
            _repo = repo;
            _waiterScheduleRepo = waiterScheduleRepo;
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
            if (dto.TimeFrom.Minute % 15 != 0 || dto.TimeFrom.Second != 0)
                throw new BusinessException("Start time must be a multiple of 15 minutes.");

            if (dto.TimeTo.Minute % 15 != 0 || dto.TimeTo.Second != 0)
                throw new BusinessException("End time must be a multiple of 15 minutes.");

            if (dto.TimeFrom == dto.TimeTo)
                throw new BusinessException("Start and end times cannot be the same.");

            var duration = CalculateDuration(dto.TimeFrom, dto.TimeTo);
            if (duration < 45)
                throw new BusinessException("Minimum booking duration is 45 minutes.");
            if (duration > 6 * 60)
                throw new BusinessException("Maximum booking duration is 6 hours.");

            var bookingStart = dto.Date.ToDateTime(dto.TimeFrom);
            if (bookingStart <= DateTime.UtcNow)
                throw new BusinessException("Cannot book for a past date or time.");

            var slots = GenerateSlots(dto.Date, dto.TimeFrom, dto.TimeTo);

            var schedule = await _waiterScheduleRepo.GetAsync(
                    $"{dto.LocationId}#{dto.TableNumber}",
                    dto.Date.ToString("yyyy-MM-dd"),
                    ct);

            var waiterId = schedule?.WaiterId ?? throw new BusinessException("No waiter assigned for this table on this date.");

            var reservation = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                WaiterId = waiterId,
                LocationId = dto.LocationId,
                TableNumber = dto.TableNumber,
                TableKey = $"{dto.LocationId}#{dto.TableNumber}",
                StartDateTime = $"{dto.Date:yyyy-MM-dd}T{dto.TimeFrom:HH:mm}Z",
                EndDateTime = $"{dto.Date:yyyy-MM-dd}T{dto.TimeTo:HH:mm}Z",
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

        private static int CalculateDuration(TimeOnly from, TimeOnly to)
        {
            if (to > from)
                return (int)(to - from).TotalMinutes;

            var toMidnight = (int)(TimeOnly.MaxValue - from).TotalMinutes + 1;
            var fromMidnight = to.Hour * 60 + to.Minute;
            return toMidnight + fromMidnight;
        }

        private static List<string> GenerateSlots(DateOnly date, TimeOnly from, TimeOnly to)
        {
            var slots = new List<string>();
            var cursor = from;
            var cursorDate = date;
            var crossMidnight = to < from;

            while (true)
            {
                slots.Add($"{cursorDate:yyyy-MM-dd}T{cursor:HH:mm}Z");

                if (cursor == to && !crossMidnight) break;
                if (cursor == to && crossMidnight) break;

                cursor = cursor.AddMinutes(15);

                if (cursor == TimeOnly.MinValue)
                    cursorDate = cursorDate.AddDays(1);

                if (slots.Count > 200)
                    throw new BusinessException("Занадто великий діапазон часу.");
            }

            return slots;
        }
    }
}
