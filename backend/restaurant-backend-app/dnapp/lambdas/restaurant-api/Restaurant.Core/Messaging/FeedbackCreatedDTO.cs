namespace Restaurant.Core.Messaging;

public sealed record FeedbackCreatedDTO(
    string ReservationId,
    int? ServiceFeedback,
    int? CuisineFeedback,
    DateTimeOffset OccurredAt
);