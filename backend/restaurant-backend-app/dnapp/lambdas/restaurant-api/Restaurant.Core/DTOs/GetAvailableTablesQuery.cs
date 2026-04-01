namespace Restaurant.Core.DTOs;

public sealed record GetAvailableTablesQuery(
    DateOnly Date,
    TimeOnly? Time,
    string? LocationId,
    int? Capacity,
    string? ExcludeReservationId);
