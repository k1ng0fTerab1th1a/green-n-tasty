using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class FeedbackService(IFeedbackRepository feedbackRepository, IReservationRepository reservationRepository, 
    IUserRepository userRepository, ILocationRepository locationRepository) : IFeedbackService
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

        if ((reservation.ServiceFeedbackId != null && dto.ServiceRating != null) ||
            (reservation.KitchenFeedbackId != null && dto.CuisineRating != null))
            return FeedbackErrors.FeedbackAlreadyMade;

        var userData = await userRepository.GetUserDataForFeedbackCreationByIdAsync(userId, ct);
        var author = new FeedbackAuthor(userId, userData.username, userData.iamgeUrl ?? string.Empty);

        return await ProcessFeedbackAsync(dto, reservation, author, ct);
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

        var result = await ProcessFeedbackAsync(dto, reservation, author, ct);
        if (result.IsFailed)
            return result;

        await reservationRepository.ClearSecretCode(reservation.Id, ct);
        return Result.Ok();
    }


    private async Task<Result> ProcessFeedbackAsync(
        CreateFeedbackDTO dto,
        Reservation reservation,
        FeedbackAuthor author,
        CancellationToken ct)
    {
        if (!dto.ServiceRating.HasValue && !dto.CuisineRating.HasValue)
            return FeedbackErrors.NoFeedbackProvided;

        if (dto.ServiceRating.HasValue)
        {
            var result = await ProcessServiceFeedbackAsync(dto, reservation, author, ct);
            if (result.IsFailed) return result;
        }

        if (dto.CuisineRating.HasValue)
        {
            var result = await ProcessCuisineFeedbackAsync(dto, reservation, author, ct);
            if (result.IsFailed) return result;
        }

        return Result.Ok();
    }

    private async Task<Result> ProcessServiceFeedbackAsync(
        CreateFeedbackDTO dto,
        Reservation reservation,
        FeedbackAuthor author,
        CancellationToken ct)
    {
        if (!dto.ServiceRating.HasValue)
            return FeedbackErrors.NoFeedbackProvided;

        if (reservation.Status < ReservationStatus.InProgress)
            return FeedbackErrors.TooEarlyServiceFeedback;

        var feedback = BuildFeedback(dto.ServiceRating.Value, dto.ServiceComment, "waiter", reservation, author);

        await feedbackRepository.SaveBatchAsync([feedback], ct);

        var res = await reservationRepository.SetFeedbackIdInReservation(reservation.Id, feedback.Id, "serviceFeedbackId", ct);
        if (res.IsFailed)
            return Result.Fail(res.Errors);

        await userRepository.UpdateUserRatingAsync(reservation.WaiterId, dto.ServiceRating.Value, ct);

        return Result.Ok();
    }

    private async Task<Result> ProcessCuisineFeedbackAsync(
        CreateFeedbackDTO dto,
        Reservation reservation,
        FeedbackAuthor author,
        CancellationToken ct)
    {
        if (!dto.CuisineRating.HasValue)
            return FeedbackErrors.NoFeedbackProvided;

        if (reservation.IsMealServed != true)
            return FeedbackErrors.MealNotYetServedForFeedback;

        var feedback = BuildFeedback(dto.CuisineRating.Value, dto.CuisineComment, "kitchen", reservation, author);

        await feedbackRepository.SaveBatchAsync([feedback], ct);

        var res = await reservationRepository.SetFeedbackIdInReservation(reservation.Id, feedback.Id, "kitchenFeedbackId", ct);
        if (res.IsFailed)
            return Result.Fail(res.Errors);

        await locationRepository.UpdateKitchenRatingAsync(reservation.LocationId, dto.CuisineRating.Value, ct);

        return Result.Ok();
    }

    public async Task<Result<WaiterLocationFeedbackDTO>> GetWaiterLocationFeedbackDtoAsync(string reservationId, bool isForUpdate, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

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

    public async Task<Result> UpdateFeedback(CreateFeedbackDTO dto, string userId, CancellationToken ct)
    {
        var cuisineViolation = ValidateRatingAndComment(dto.CuisineRating, dto.CuisineComment);
        if (cuisineViolation != null) return cuisineViolation;

        var serviceViolation = ValidateRatingAndComment(dto.ServiceRating, dto.ServiceComment);
        if (serviceViolation != null) return serviceViolation;

        if (!dto.CuisineRating.HasValue && !dto.ServiceRating.HasValue)
            return FeedbackErrors.NoFeedbackUpdated;

        var reservation = await reservationRepository.GetByIdAsync(dto.ReservationId, ct);
        if (reservation == null)
            return ReservationErrors.ReservationNotFound;

        FeedbackAuthor? author = null;

        if (dto.CuisineRating.HasValue)
        {
            var result = string.IsNullOrEmpty(reservation.KitchenFeedbackId)
                ? await ProcessCuisineFeedbackAsync(dto, reservation, author = await GetAuthorAsync(userId, ct), ct)
                : await UpdateExistingKitchenFeedbackAsync(reservation.KitchenFeedbackId, reservation.LocationId, dto.CuisineRating.Value, dto.CuisineComment, ct);

            if (result.IsFailed) return result;
        }

        if (dto.ServiceRating.HasValue)
        {
            var result = string.IsNullOrEmpty(reservation.ServiceFeedbackId)
                ? await ProcessServiceFeedbackAsync(dto, reservation, author ?? await GetAuthorAsync(userId, ct), ct)
                : await UpdateExistingServiceFeedbackAsync(reservation.ServiceFeedbackId, reservation.WaiterId, dto.ServiceRating.Value, dto.ServiceComment, ct);

            if (result.IsFailed) return result;
        }

        return Result.Ok();
    }
    
    private async Task<Result> UpdateExistingKitchenFeedbackAsync(
    string feedbackId, string locationId, int newRating, string? newComment, CancellationToken ct)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId, ct);
        if (feedback == null) return FeedbackErrors.FeedbackNotFound;

        var location = await locationRepository.GetByIdAsync(locationId, ct);
        if (location == null) return ReservationErrors.LocationNotFound;

        location.TotalRating = RecalculateRating(location.TotalRating, feedback.Rate, newRating);
        feedback.Comment     = newComment;
        feedback.Rate        = newRating;

        try
        {
            await Task.WhenAll(
                locationRepository.UpdateAsync(location, ct),
                feedbackRepository.UpdateFeedback(feedback, ct)
            );
            return Result.Ok();
        }
        catch (Exception)
        {
            return FeedbackErrors.FeedbackUpdateUnsuccessful;
        }
    }

    private async Task<Result> UpdateExistingServiceFeedbackAsync(
        string feedbackId, string waiterId, int newRating, string? newComment, CancellationToken ct)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId, ct);
        if (feedback == null) return FeedbackErrors.FeedbackNotFound;

        var waiter = await userRepository.GetByIdAsync(waiterId, ct);
        if (waiter == null) return ReservationErrors.WaiterNotFound;

        waiter.TotalRating = RecalculateRating(waiter.TotalRating, feedback.Rate, newRating);
        feedback.Comment   = newComment;
        feedback.Rate      = newRating;

        try
        {
            await Task.WhenAll(
                userRepository.CreateAsync(waiter, ct, true),
                feedbackRepository.UpdateFeedback(feedback, ct)
            );
            return Result.Ok();
        }
        catch (Exception)
        {
            return FeedbackErrors.FeedbackUpdateUnsuccessful;
        }
    }

    private async Task<FeedbackAuthor> GetAuthorAsync(string userId, CancellationToken ct)
    {
        var userData = await userRepository.GetUserDataForFeedbackCreationByIdAsync(userId, ct);
        return new FeedbackAuthor(userId, userData.username, userData.iamgeUrl ?? string.Empty);
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
    
    private int RecalculateRating(int currentTotal, int oldRate, int newRate)
        => currentTotal - oldRate + newRate;
    
    
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