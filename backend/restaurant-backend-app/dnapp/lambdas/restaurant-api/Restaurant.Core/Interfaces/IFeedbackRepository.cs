using Restaurant.Core.ServiceDTOs;

namespace Restaurant.Core.Interfaces;

public interface IFeedbackRepository
{
    public Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type = "waiter", List<string>? sort = null, string? pageToken = null);
}