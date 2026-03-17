namespace Restaurant.Core.DTOs;

public sealed class CreateReservationForWaiterDTO
{
    public CreateReservationForWaiterDTO(
        string locationId,
        int tableNumber,
        DateOnly date,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        int guestsCount,
        string? customerId,
        string? visitorName)
    {
        LocationId = locationId;
        TableNumber = tableNumber;
        Date = date;
        TimeFrom = timeFrom;
        TimeTo = timeTo;
        GuestsCount = guestsCount;
        CustomerId = customerId;
        VisitorName = visitorName;
    }

    public string LocationId { get; set; } = null!;
    public int TableNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    public int GuestsCount { get; set; }
    public string? CustomerId { get; set; }
    public string? VisitorName { get; set; }
}