using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Interfaces;
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
    }
}
