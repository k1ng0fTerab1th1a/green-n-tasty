namespace Restaurant.Core.Errors;

public static class FeedbackErrors
{
    public static BusinessError ReservationUnauthorizedAccess =>
        new("This reservation does not belong to you", ErrorType.Unauthorized);

    public static BusinessError TooEarlyServiceFeedback =>
        new("You can leave feedback only after the reservation was set in progress", ErrorType.Validation);
    
    public static BusinessError MealNotYetServedForFeedback =>
        new("You can leave feedback only after the meal was served", ErrorType.Validation);

    public static BusinessError NoFeedbackProvided =>
        new("No feedback was provided", ErrorType.Validation);
    
    public static BusinessError RatingsNotFound =>
        new("Ratings were not found", ErrorType.NotFound);

    public static BusinessError DataFetchingError =>
        new("Something went wrong during data fetching", ErrorType.Validation);
    
    public static BusinessError UnsuccessfulRatingUpdate =>
        new("Rating update was unsuccessful", ErrorType.Validation);
}