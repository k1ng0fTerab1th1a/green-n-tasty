namespace Restaurant.Core.DTOs;

public sealed record ReservationCompletedDTO(
    string ReservationId,
    DateTimeOffset OccurredAt
);