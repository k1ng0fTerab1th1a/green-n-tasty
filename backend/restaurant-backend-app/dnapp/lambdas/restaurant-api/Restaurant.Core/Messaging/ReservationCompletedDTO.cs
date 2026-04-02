namespace Restaurant.Core.Messaging;

public sealed record ReservationCompletedDTO(
    string ReservationId,
    DateTimeOffset OccurredAt
);
