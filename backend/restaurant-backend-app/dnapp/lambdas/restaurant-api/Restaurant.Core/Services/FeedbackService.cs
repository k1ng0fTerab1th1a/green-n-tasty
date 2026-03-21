using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Core.Services;

public class FeedbackService(IFeedbackRepository feedbackRepository) : IFeedbackService
{
    public async Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(string locationId, int size, string type, List<string> sort, string? pageToken = null)
    {
        FeedbackPaginatedDto result = new FeedbackPaginatedDto();
        var receivedFeedbacks = await feedbackRepository.GetByLocationAsync(locationId, size, type, sort, pageToken);

        result.Size = size;
        result.NextPageToken = receivedFeedbacks.NextPageToken;

        if (receivedFeedbacks.Feedbacks.Count > 0)
        {
            // TODO: Replace user data getting and setting with the actual user data getting
            for (int i = 0; i < receivedFeedbacks.Feedbacks.Count; i++)
            {
                FeedbackDTO feedback = new FeedbackDTO(receivedFeedbacks.Feedbacks[i]);
                result.Content.Add(feedback);
            }
        }
        return Result.Ok(result);
    }
}