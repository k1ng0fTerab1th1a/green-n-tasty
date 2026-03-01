using Restaurant.Core.ServiceDTOs;

namespace Restaurant.Core.Interfaces;

public interface IFeedbackRepository
{
    Task SaveAsync(Feedback feedback);

    Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type, string? pageToken = null);
}