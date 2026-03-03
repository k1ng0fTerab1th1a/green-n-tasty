using Restaurant.Core.ServiceDTOs;

namespace Restaurant.Core.Interfaces;

public interface IFeedbackService
{
    Task<FeedbackPaginatedDto> GetFeedbacksForLocation(string locationId, int size, string type, 
        List<string> sort, string? pageToken = null);
}