using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IReservationService
{
    Task<Result<Reservation>> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct);
    Task<Result<IReadOnlyList<Reservation>>> GetByCustomer(string actorUserId, CancellationToken ct);
    Task<Result<IReadOnlyList<Reservation>>> GetByWaiter(string actorUserId, DateOnly? date, CancellationToken ct);
    Task<Result> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct);
    Task<Result<Reservation>> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct);
    Task<Result<Reservation>> CreateForWaiterAsync(string waiterId, CreateReservationForWaiterDTO dto, CancellationToken ct);
    Task<Result<IReadOnlyList<WaiterCustomerLookupDTO>>> SearchCustomersForWaiterAsync(string actorUserId, string query, CancellationToken ct);
    Task<Result<Reservation>> UpdateReservationAsync(string actorUserId, bool isActorWaiter, UpdateReservationDTO dto, CancellationToken ct);
    Task<Result<Reservation>> StartReservationAsync(string reservationId, string waiterId, CancellationToken ct);
    Task<Result<Reservation>> MarkMealsServedAsync(string reservationId, string waiterId, CancellationToken ct);
    Task<Result<Reservation>> FinishReservationAsync(string reservationId, string waiterId, CancellationToken ct);
}
