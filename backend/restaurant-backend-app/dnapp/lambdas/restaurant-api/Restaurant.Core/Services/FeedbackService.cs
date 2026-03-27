using FluentResults;
using Microsoft.Extensions.Options;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Services;

public class FeedbackService(IFeedbackRepository feedbackRepository, IReservationRepository reservationRepository, 
    IUserRepository userRepository, ILocationRepository locationRepository, IOptions<ClientSettings> options) : IFeedbackService
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

    public async Task<Result> SaveAnonymousFeedback(CreateFeedbackDTO req, CancellationToken ct = default)
    {
        return null;
    }

    public async Task<Result> SaveAuthorisedFeedback(CreateFeedbackDTO dto, string userId, CancellationToken ct = 
            default)
    {
        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return Result.Fail("Reservation not found");
        
        if (reservation.CustomerId != userId)
            return Result.Fail("Reservation does not belong to You");


        var userData = await userRepository.GetUserDataForFeedbackCreationByIdAsync(userId, ct);
        List<Feedback> feedbacksToSave = new List<Feedback>();

        var ratingUpdates = new List<Func<CancellationToken, Task>>();

        if (dto.ServiceRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.InProgress)
            {
                return Result.Fail("Service feedback is only available once the reservation is in progres");
            }
            
            var serviceFeedback = new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                Rate = dto.ServiceRating.Value,
                Comment = dto.ServiceComment ?? string.Empty,
                UserId = userId,
                UserName = userData.username,
                UserAvatarUrl = userData.iamgeUrl ?? string.Empty,
                Date = DateTime.UtcNow.ToString("o"),
                LocationId = reservation.LocationId,
                LocationIdAndType = $"{reservation.LocationId}#waiter",
                Type = "waiter"
            };
            
            feedbacksToSave.Add(serviceFeedback);
            
            ratingUpdates.Add(ct => UpdateWaiterRatingAsync(reservation.WaiterId, dto.ServiceRating!.Value, ct));
        }
        
        if (dto.CuisineRating.HasValue)
        {
            if (reservation.Status <ReservationStatus.MealsServed)
                return Result.Fail("Cuisine feedback is only available once meals have been served");

            var cuisineFeedback = new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                Rate = dto.CuisineRating.Value,
                Comment = dto.CuisineComment ?? string.Empty,
                UserId = userId,
                UserName = userData.username,
                UserAvatarUrl = userData.iamgeUrl ?? string.Empty,
                Date = DateTime.UtcNow.ToString("o"),
                LocationId = reservation.LocationId,
                LocationIdAndType = $"{reservation.LocationId}#kitchen",
                Type = "kitchen"
            };

            feedbacksToSave.Add(cuisineFeedback);
            
            ratingUpdates.Add(ct => UpdateLocationRatingAsync(reservation.LocationId, dto.CuisineRating!.Value, ct));
        }
        
        if (!feedbacksToSave.Any())
            return Result.Fail("No feedback provided");

        await feedbackRepository.SaveBatchAsync(feedbacksToSave, ct);

        if (ratingUpdates.Any())
        {
            try
            {
                await Task.WhenAll(ratingUpdates.Select(update => update(ct)));
            }
            catch (Exception ex)
            {
                return Result.Fail("Rating update was not successful");
            }
        }
        return Result.Ok();
    }

    public async Task<Result<byte[]>> GenerateFeedbackQr(string reservationId, CancellationToken ct = default)
    {
        var secretCode = await feedbackRepository.GetSecretCodeByReservationIdAsync(reservationId, ct);
        if (string.IsNullOrEmpty(secretCode))
        {
            return Result.Fail("Secret code for this reservation was not received");
        }
        
        string combinedUrl = options.Value.ClientUrl + "/feedback" + 
            $"?reservationId={reservationId}&secretCode={secretCode}";

        QrCoder coder = new QrCoder();
        var qrCode = coder.GenerateQrCode(combinedUrl);
        return Result.Ok(qrCode);
    }
    
    private async Task UpdateWaiterRatingAsync(string waiterId, int rating, CancellationToken ct)
    {
        await userRepository.UpdateUserRatingAsync(waiterId, rating, ct);
    }

    private async Task UpdateLocationRatingAsync(string locationId, int rating, CancellationToken ct)
    {
        await locationRepository.UpdateUserRatingAsync(locationId, rating, ct);
    }
}