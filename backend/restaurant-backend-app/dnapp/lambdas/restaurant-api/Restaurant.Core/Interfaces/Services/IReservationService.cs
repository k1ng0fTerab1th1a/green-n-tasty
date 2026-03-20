using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IReservationService
{
    Task<Result<Reservation>> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetMyAsync(string actorUserId, bool actorIsWaiter, CancellationToken ct = default);
    Task<Result> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct = default);
    Task<Result<Reservation>> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default);
    Task<Result<Reservation>> CreateForWaiterAsync(string waiterId, CreateReservationForWaiterDTO dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WaiterCustomerLookupDTO>>> SearchCustomersForWaiterAsync(string actorUserId, string query, CancellationToken ct = default);
    Task<Result<Reservation>> UpdateReservationAsync(string actorUserId, bool isActorWaiter, UpdateReservationDTO dto, CancellationToken ct = default);
}
