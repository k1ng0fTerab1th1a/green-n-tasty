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

    public static BusinessError FeedbackAlreadyMade =>
        new("You have already left your feedback for that", ErrorType.Validation);

    public static BusinessError EmptyFeedbackCreationBody =>
        new("You have provided no information for leaving the feedback", ErrorType.Validation);

    public static BusinessError RatingValidationDiapasonError =>
        new("Rating should be within 1 and 5 stars", ErrorType.Validation);

    public static BusinessError FeedbackCommentSizeOutOfBounds =>
        new("Comment should not consist of more than 300 characters", ErrorType.Validation);
    
    public static BusinessError FeedbackNotFound =>
        new("Feedback not found", ErrorType.NotFound);
    
    public static BusinessError FeedbackUpdateUnsuccessful =>
        new("Feedback update unsuccessful", ErrorType.Validation);
    
}