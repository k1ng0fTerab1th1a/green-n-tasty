using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task SaveBatchAsync(IEnumerable<Feedback> feedbacks, CancellationToken ct);
    Task<string?> GetSecretCodeByReservationIdAsync(string reservationId, CancellationToken ct);
    Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type, List<string>? sort, string? pageToken, CancellationToken ct);

    Task<bool> IsFeedbackAlreadyMade(string reservationId, string feedbackType, CancellationToken ct);

    Task<Feedback?> GetByIdAsync(string feedbackId, CancellationToken ct);
    Task UpdateFeedback(Feedback feedback, CancellationToken ct);
}