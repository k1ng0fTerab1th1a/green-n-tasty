using System.Text;
using Amazon.Runtime;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces;
using Restaurant.Core.ServiceDTOs;

namespace Restaurant.Core.Services;

public class FeedbackService(IFeedbackRepository feedbackRepository, IUserRepository userRepository) : IFeedbackService
{
    public async Task<FeedbackPaginatedDto> GetFeedbacksForLocation(string locationId, int size, string type, List<string> sort, string? pagetToken = null)
    {
        FeedbackPaginatedDto result = new FeedbackPaginatedDto();
        var receivedFeedbacks = await feedbackRepository.GetByLocationAsync(locationId, size, type, pagetToken);

        result.TotalPages = receivedFeedbacks.TotalPages;
        result.TotalElements = receivedFeedbacks.TotalSize;
        result.Size = size;
        result.NextPageToken = receivedFeedbacks.NextPageToken;

        if (receivedFeedbacks.Feedbacks.Count > 0)
        {
            // TODO: Replace user data getting and setting with the actual user data getting
            for (int i = 0; i < receivedFeedbacks.Feedbacks.Count; i++)
            {
                FeedbackDTO feedback = new FeedbackDTO(receivedFeedbacks.Feedbacks[i]);
                feedback.UserName = "Test User " + i.ToString();
                feedback.UserAvatarUrl = "TestUserAvatar.jpg" + i.ToString();
                result.Content.Add(feedback);
            }
        }

        result.Content = SortHelper.SortFeedbackDynamic(result.Content, sort);
        
        return result;
    }
}