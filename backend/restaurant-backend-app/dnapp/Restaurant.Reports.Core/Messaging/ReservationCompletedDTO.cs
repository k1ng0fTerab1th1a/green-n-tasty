namespace Restaurant.Reports.Messaging;

public sealed record ReservationCompletedDTO(
    string ReservationId,
    DateTimeOffset OccurredAt
);
