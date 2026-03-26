using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IFeedbackRepository
{
    public Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type = "waiter", List<string>? sort = null, string? pageToken = null);

    Task SaveBatchAsync(IEnumerable<Feedback> feedbacks, CancellationToken ct = default);
    Task<string?> GetSecretCodeByReservationIdAsync(string reservationId, CancellationToken ct = default);
}