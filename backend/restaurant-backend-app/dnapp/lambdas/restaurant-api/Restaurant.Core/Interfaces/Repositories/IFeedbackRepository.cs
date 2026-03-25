using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type, List<string>? sort, string? pageToken, CancellationToken ct);
}