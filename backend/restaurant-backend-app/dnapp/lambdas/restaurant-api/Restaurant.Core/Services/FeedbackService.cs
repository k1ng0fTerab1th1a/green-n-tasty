using FluentResults;
using Microsoft.Extensions.Options;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Services;

public class FeedbackService(IFeedbackRepository feedbackRepository, IReservationRepository reservationRepository, 
    IUserRepository userRepository, ILocationRepository locationRepository, IOptions<ClientSettings> options) : IFeedbackService
{
    public async Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(string locationId, int size, string type, List<string> sort, string? pageToken = null, CancellationToken ct = default)
    {
        FeedbackPaginatedDto result = new FeedbackPaginatedDto();
        var receivedFeedbacks = await feedbackRepository.GetByLocationAsync(locationId, size, type, sort, pageToken, ct);

        result.Size = size;
        result.NextPageToken = receivedFeedbacks.NextPageToken;

        if (receivedFeedbacks.Feedbacks.Count > 0)
        {
            for (int i = 0; i < receivedFeedbacks.Feedbacks.Count; i++)
            {
                FeedbackDTO feedback = new FeedbackDTO(receivedFeedbacks.Feedbacks[i]);
                result.Content.Add(feedback);
            }
        }
        return Result.Ok(result);
    }

    public async Task<Result> SaveAuthorisedFeedback(CreateFeedbackDTO dto, string userId, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        if (reservation.CustomerId != userId)
            return FeedbackErrors.ReservationUnauthorizedAccess;


        var userData = await userRepository.GetUserDataForFeedbackCreationByIdAsync(userId, ct);
        List<Feedback> feedbacksToSave = new List<Feedback>();

        var ratingUpdates = new List<Func<CancellationToken, Task>>();

        if (dto.ServiceRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.InProgress)
            {
                return FeedbackErrors.TooEarlyServiceFeedback;
            }
            
            bool serviceFeedbackLeft = await feedbackRepository.IsFeedbackAlreadyMade(reservation.Id, "service", ct);
            if (serviceFeedbackLeft)
            {
                return FeedbackErrors.FeedbackAlreadyMade;
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

            var userRatingData = await userRepository.GetUserFeedbackRatingDataByIdAsync(reservation.WaiterId, ct);
            if (userRatingData.rating < 0 || userRatingData.feedbacksAmount == -1)
            {
                return FeedbackErrors.RatingsNotFound;
            }
            
            
            ratingUpdates.Add(ct => UpdateWaiterRatingAsync(reservation.WaiterId, dto.ServiceRating!.Value, ct));
        }
        
        if (dto.CuisineRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.MealsServed)
                return FeedbackErrors.MealNotYetServedForFeedback;

            bool serviceFeedbackLeft = await feedbackRepository.IsFeedbackAlreadyMade(reservation.Id, "kitchen", ct);
            if (serviceFeedbackLeft)
            {
                return FeedbackErrors.FeedbackAlreadyMade;
            }
            
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
            return FeedbackErrors.NoFeedbackProvided;

        await feedbackRepository.SaveBatchAsync(feedbacksToSave, ct);

        if (ratingUpdates.Count != 0)
        {
            try
            {
                await Task.WhenAll(ratingUpdates.Select(update => update(ct)));
            }
            catch (Exception ex)
            {
                return FeedbackErrors.UnsuccessfulRatingUpdate;
            }
        }
        return Result.Ok();
    }

    public async Task<Result> SaveVisitorFeedback(CreateFeedbackDTO dto, string secretCode,
        CancellationToken ct = default)
    {
        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        if (string.IsNullOrEmpty(reservation.SecretCode) || reservation.SecretCode != secretCode)
            return FeedbackErrors.ReservationUnauthorizedAccess;
        
        List<Feedback> feedbacksToSave = new List<Feedback>(); 
        var ratingUpdates = new List<Func<CancellationToken, Task>>();
        
        if (dto.ServiceRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.InProgress)
            {
                return FeedbackErrors.TooEarlyServiceFeedback;
            }
            
            var serviceFeedback = new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                Rate = dto.ServiceRating.Value,
                Comment = dto.ServiceComment ?? string.Empty,
                UserId = string.Empty,
                UserName = "Visitor",
                UserAvatarUrl = string.Empty,
                Date = DateTime.UtcNow.ToString("o"),
                LocationId = reservation.LocationId,
                LocationIdAndType = $"{reservation.LocationId}#waiter",
                Type = "waiter"
            };
            
            feedbacksToSave.Add(serviceFeedback);

            var userRatingData = await userRepository.GetUserFeedbackRatingDataByIdAsync(reservation.WaiterId, ct);
            if (userRatingData.rating < 0 || userRatingData.feedbacksAmount == -1)
            {
                return FeedbackErrors.RatingsNotFound;
            }
            
            
            ratingUpdates.Add(ct => UpdateWaiterRatingAsync(reservation.WaiterId, dto.ServiceRating!.Value, ct));
        }
        
        if (dto.CuisineRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.MealsServed)
                return FeedbackErrors.MealNotYetServedForFeedback;

            
            var cuisineFeedback = new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                Rate = dto.CuisineRating.Value,
                Comment = dto.CuisineComment ?? string.Empty,
                UserId = string.Empty,
                UserName = "Visitor",
                UserAvatarUrl = string.Empty,
                Date = DateTime.UtcNow.ToString("o"),
                LocationId = reservation.LocationId,
                LocationIdAndType = $"{reservation.LocationId}#kitchen",
                Type = "kitchen"
            };

            feedbacksToSave.Add(cuisineFeedback);
            
            
            ratingUpdates.Add(ct => UpdateLocationRatingAsync(reservation.LocationId, dto.CuisineRating!.Value, ct));
        }

        if (feedbacksToSave.Count == 0)
            return FeedbackErrors.NoFeedbackProvided;

        await feedbackRepository.SaveBatchAsync(feedbacksToSave, ct);

        if (ratingUpdates.Count != 0)
        {
            try
            {
                await Task.WhenAll(ratingUpdates.Select(update => update(ct)));
            }
            catch (Exception)
            {
                return FeedbackErrors.UnsuccessfulRatingUpdate;
            }
        }

        await reservationRepository.ClearSecretCode(reservation.Id, ct);
        
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

    public async Task<Result<WaiterLocationFeedbackDTO>> GetCalculatedFeedbackDataAsync(string reservationId,
        CancellationToken ct = default)
    {
        var ids = await reservationRepository.GetWaiterAndLocationIdFromReservationAsync(reservationId, ct);
        if (string.IsNullOrEmpty(ids.locationId) || string.IsNullOrEmpty(ids.waiterId))
            return FeedbackErrors.DataFetchingError;

        var cuisineRatingData = await locationRepository.GetLocationFeedbacksDataAsync(ids.locationId, ct);

        double cuisineRating = cuisineRatingData.rating / (double)cuisineRatingData.feedbacksAmount;

        var waiterRatingData = await userRepository.GetWaiterFeedbackDataAsync(ids.waiterId, ct);

        double waiterRating = waiterRatingData.WaiterRating / (double)waiterRatingData.WaiterFeedbacksNumber;

        return Result.Ok(new WaiterLocationFeedbackDTO()
        {
            WaiterFeedbacksNumber = waiterRatingData.WaiterFeedbacksNumber,
            WaiterRating = waiterRating,
            WaiterImageUrl = waiterRatingData.WaiterImageUrl,
            WaiterName = waiterRatingData.WaiterName,
            CuisineFeedbacksNumber = cuisineRatingData.feedbacksAmount,
            CuisineRating = cuisineRating
        });
    }
    
    private async Task UpdateWaiterRatingAsync(string waiterId, int newRating, CancellationToken ct)
    {
        await userRepository.UpdateUserRatingAsync(waiterId, newRating, ct);
    }

    private async Task UpdateLocationRatingAsync(string locationId, int newRating, CancellationToken ct)
    {
        await locationRepository.UpdateKitchenRatingAsync(locationId, newRating, ct);
    }
}