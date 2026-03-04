using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces
{
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
    }
}
