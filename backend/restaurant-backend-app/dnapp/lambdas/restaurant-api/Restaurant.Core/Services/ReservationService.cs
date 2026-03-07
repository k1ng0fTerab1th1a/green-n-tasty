using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services
{
    public sealed class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repo;

        public ReservationService(IReservationRepository repo)
        {
            _repo = repo;
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
            var reservation = await _repo.GetByIdAsync(reservationId, ct);
            if (reservation == null)
            {
                throw new ArgumentNullException("reservation", "Reservation does not exist");
            }
            

            var allowed = reservation.CustomerId == userId || 
                          (isWaiter && reservation.WaiterId == userId);
            if (!allowed) throw new UnauthorizedAccessException("Forbidden.");

            var startTime = DateTime.Parse(reservation.StartDateTime, null, DateTimeStyles.RoundtripKind);
            if ((startTime - DateTime.UtcNow).TotalMinutes < 30)
                throw new InvalidOperationException("Reservation cannot be cancelled less than 30 minutes before it starts.");
            
            var slots = GenerateSlots(DateTime.Parse(reservation.StartDateTime),
                DateTime.Parse(reservation.EndDateTime));

            bool result = await _repo.DeleteReservationAsync(reservation, slots, ct);
            if (!result)
            {
                // TODO: Change to custom exception
                throw new Exception("Something went wrong");
            }

            return true;
        }
        
        private static List<string> GenerateSlots(DateTime start, DateTime end)
        {
            var slots = new List<string>();
            var cursor = start;

            while (cursor <= end)
            {
                slots.Add(cursor.ToString("yyyy-MM-ddTHH:mmZ"));

                if (cursor == end) break;

                cursor = cursor.AddMinutes(15);

                if (slots.Count > 200)
                    // TODO: Change to custom exception
                    throw new Exception("Too big time diapason");
            }

            return slots;
        }
    }
}
