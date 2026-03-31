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
        var res = ValidateFeedbackData(dto);
        if (res.IsFailed)
            return res;
        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        if (reservation.CustomerId != userId)
            return FeedbackErrors.ReservationUnauthorizedAccess;

        var userData = await userRepository.GetUserDataForFeedbackCreationByIdAsync(userId, ct);
        var author = new FeedbackAuthor(userId, userData.username, userData.iamgeUrl ?? string.Empty);

        return await ProcessFeedbackAsync(dto, reservation, author, checkDuplicates: true, ct);
    }

    public async Task<Result> SaveVisitorFeedback(CreateFeedbackDTO dto, string secretCode, CancellationToken ct = default)
    {
        var res = ValidateFeedbackData(dto);
        if (res.IsFailed)
            return res;
        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        if (string.IsNullOrEmpty(reservation.SecretCode) || reservation.SecretCode != secretCode)
            return FeedbackErrors.ReservationUnauthorizedAccess;

        var author = new FeedbackAuthor(string.Empty, "Visitor", string.Empty);

        var result = await ProcessFeedbackAsync(dto, reservation, author, checkDuplicates: false, ct);
        if (result.IsFailed)
            return result;

        await reservationRepository.ClearSecretCode(reservation.Id, ct);
        return Result.Ok();
    }


    private async Task<Result> ProcessFeedbackAsync(
        CreateFeedbackDTO dto,
        Reservation reservation,
        FeedbackAuthor author,
        bool checkDuplicates,
        CancellationToken ct)
    {

        var feedbacksToSave = new List<Feedback>();
        var ratingUpdates   = new List<Func<CancellationToken, Task>>();

        if (dto.ServiceRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.InProgress)
                return FeedbackErrors.TooEarlyServiceFeedback;

            if (checkDuplicates && await feedbackRepository.IsFeedbackAlreadyMade(reservation.Id, "waiter", ct))
                return FeedbackErrors.FeedbackAlreadyMade;

            feedbacksToSave.Add(BuildFeedback(dto.ServiceRating.Value, dto.ServiceComment, "waiter", reservation, author));
            ratingUpdates.Add(c => userRepository.UpdateUserRatingAsync(reservation.WaiterId, dto.ServiceRating.Value, 
                c));
        }

        if (dto.CuisineRating.HasValue)
        {
            if (reservation.Status < ReservationStatus.MealsServed)
                return FeedbackErrors.MealNotYetServedForFeedback;

            if (checkDuplicates && await feedbackRepository.IsFeedbackAlreadyMade(reservation.Id, "kitchen", ct))
                return FeedbackErrors.FeedbackAlreadyMade;

            feedbacksToSave.Add(BuildFeedback(dto.CuisineRating.Value, dto.CuisineComment, "kitchen", reservation, author));
            ratingUpdates.Add(c => locationRepository.UpdateKitchenRatingAsync(reservation.LocationId, dto.CuisineRating.Value, c));
        }

        if (feedbacksToSave.Count == 0)
            return FeedbackErrors.NoFeedbackProvided;

        await feedbackRepository.SaveBatchAsync(feedbacksToSave, ct);

        if (ratingUpdates.Count > 0)
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

    public async Task<Result<WaiterLocationFeedbackDTO>> GetWaiterLocationFeedbackDTOAsync(string reservationId, bool isForUpdate, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        // cuisine, waiter, and (optionally) both feedbacks — all in parallel
        var cuisineTask = locationRepository.GetLocationFeedbacksDataAsync(reservation.LocationId, ct);
        var waiterTask  = userRepository.GetWaiterFeedbackDataAsync(reservation.WaiterId, ct);

        var kitchenFeedbackTask = isForUpdate && !string.IsNullOrEmpty(reservation.KitchenFeedbackId)
            ? feedbackRepository.GetByIdAsync(reservation.KitchenFeedbackId, ct)
            : Task.FromResult<Feedback?>(null);

        var serviceFeedbackTask = isForUpdate && !string.IsNullOrEmpty(reservation.ServiceFeedbackId)
            ? feedbackRepository.GetByIdAsync(reservation.ServiceFeedbackId, ct)
            : Task.FromResult<Feedback?>(null);

        await Task.WhenAll(cuisineTask, waiterTask, kitchenFeedbackTask, serviceFeedbackTask);

        var cuisineRatingData = await cuisineTask;
        var waiterRatingData  = await waiterTask;
        var kitchenFeedback   = await kitchenFeedbackTask;
        var serviceFeedback   = await serviceFeedbackTask;

        var resultDto = new WaiterLocationFeedbackDTO
        {
            CuisineRating          = cuisineRatingData.feedbacksAmount <= 0 ? 0 : cuisineRatingData.rating / (double)cuisineRatingData.feedbacksAmount,
            CuisineFeedbacksNumber = cuisineRatingData.feedbacksAmount,
            WaiterRating           = waiterRatingData.WaiterFeedbacksNumber <= 0 ? 0 : waiterRatingData.WaiterRating / (double)waiterRatingData.WaiterFeedbacksNumber,
            WaiterFeedbacksNumber  = waiterRatingData.WaiterFeedbacksNumber,
            WaiterName             = waiterRatingData.WaiterName,
            WaiterImageUrl         = waiterRatingData.WaiterImageUrl
        };

        if (kitchenFeedback != null || serviceFeedback != null)
        {
            resultDto.UpdateUserData = new FeedbackOfUserDTO();

            if (kitchenFeedback != null)
            {
                resultDto.UpdateUserData.KitchenFeedbackId = reservation.KitchenFeedbackId;
                resultDto.UpdateUserData.KitchenComment    = kitchenFeedback.Comment;
                resultDto.UpdateUserData.KitchenRating     = kitchenFeedback.Rate;
            }

            if (serviceFeedback != null)
            {
                resultDto.UpdateUserData.ServiceFeedbackId = reservation.ServiceFeedbackId;
                resultDto.UpdateUserData.ServiceComment    = serviceFeedback.Comment;
                resultDto.UpdateUserData.ServiceRating     = serviceFeedback.Rate;
            }
        }

        return Result.Ok(resultDto);
    }

    public async Task<Result> UpdateFeedback(string feedbackId, string comment, int rating, string feedbackType,
        CancellationToken ct)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId, ct);
        if (feedback == null)
            return FeedbackErrors.FeedbackNotFound;

        var validation = ValidateRatingAndComment(rating, comment);
        if (validation != null)
            return validation;
        
        var reservation = await reservationRepository.GetByIdAsync(feedback.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        if (feedbackType == "waiter")
        {
            var waiter = await userRepository.GetByIdAsync(reservation.WaiterId, ct);
            if (waiter == null)
                return ReservationErrors.WaiterNotFound;

            waiter.TotalRating = waiter.TotalRating - feedback.Rate + rating;
        }
        else if (feedbackType == "kitchen")
        {
            var location = await locationRepository.GetByIdAsync(reservation.LocationId, ct);
            if (location == null)
                return ReservationErrors.LocationNotFound;

            location.TotalRating = location.TotalRating - feedback.Rate + rating;
        }

        feedback.Rate = rating;
        feedback.Comment = comment;
        // here I will need to update feedback and then update user or location somehow
        try
        {
            await feedbackRepository.UpdateFeedback(feedback, ct);
            return Result.Ok();
        }
        catch (Exception)
        {
            return FeedbackErrors.FeedbackUpdateUnsuccessful;
        }
    }
    
    private static Result ValidateFeedbackData(CreateFeedbackDTO dto)
    {
        if (dto.CuisineRating == null && dto.ServiceRating == null)
            return FeedbackErrors.NoFeedbackProvided;

        return ValidateRatingAndComment(dto.CuisineRating, dto.CuisineComment)
               ?? ValidateRatingAndComment(dto.ServiceRating, dto.ServiceComment)
               ?? Result.Ok();
    }

    private static Result? ValidateRatingAndComment(int? rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            return FeedbackErrors.RatingValidationDiapasonError;

        if (!string.IsNullOrEmpty(comment) && comment.Length > 300)
            return FeedbackErrors.FeedbackCommentSizeOutOfBounds;

        return null;
    }
    
    
    private static Feedback BuildFeedback(
        int rate,
        string? comment,
        string type,
        Reservation reservation,
        FeedbackAuthor author) => new()
    {
        Id                = Guid.NewGuid().ToString(),
        Rate              = rate,
        Comment           = comment ?? string.Empty,
        UserId            = author.UserId,
        UserName          = author.UserName,
        UserAvatarUrl     = author.AvatarUrl,
        Date              = DateTime.UtcNow.ToString("o"),
        LocationId        = reservation.LocationId,
        LocationIdAndType = $"{reservation.LocationId}#{type}",
        Type              = type,
    };
    private record FeedbackAuthor(string UserId, string UserName, string AvatarUrl);
}