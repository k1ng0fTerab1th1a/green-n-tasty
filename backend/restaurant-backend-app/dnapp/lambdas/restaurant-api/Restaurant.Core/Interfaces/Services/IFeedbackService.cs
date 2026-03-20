using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface IFeedbackService
{
    Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(string locationId, int size, string type,
        List<string> sort, string? pageToken = null);
}