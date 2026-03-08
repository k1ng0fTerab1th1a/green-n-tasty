using Restaurant.Core.DTOs;
using Restaurant.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Interfaces.Services
{
    public interface IReservationService
    {
        Task<Reservation?> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default);
        Task<IReadOnlyList<Reservation>> GetMyAsync(string actorUserId, bool actorIsWaiter, CancellationToken ct = default);
        Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default);
    }
}
