namespace Restaurant.Core.Errors;

public static class FeedbackErrors
{
    public static BusinessError ReservationUnauthorizedAccess =>
        new("This reservation does not belong to you", ErrorType.Unauthorized);

    public static BusinessError TooEarlyServiceFeedback =>
        new("You can set feedback only after the reservation was set in progress", ErrorType.Validation);
    
    public static BusinessError RatingsNotFound =>
        new("Ratings were not found", ErrorType.NotFound);
}