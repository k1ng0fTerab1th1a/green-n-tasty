namespace Restaurant.Core.DTOs;

public sealed record CreateReservationDTO(
    string LocationId,
    int TableNumber,
    DateOnly Date,
    TimeOnly TimeFrom,
    TimeOnly TimeTo,
    int GuestsCount
);
