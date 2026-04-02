namespace Restaurant.Core.DTOs;

public sealed class CreateReservationForWaiterDTO
{
    public CreateReservationForWaiterDTO(
        int tableNumber,
        DateOnly date,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        int guestsCount,
        string? customerId,
        string? visitorName)
    {
        TableNumber = tableNumber;
        Date = date;
        TimeFrom = timeFrom;
        TimeTo = timeTo;
        GuestsCount = guestsCount;
        CustomerId = customerId;
        VisitorName = visitorName;
    }

    public int TableNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    public int GuestsCount { get; set; }
    public string? CustomerId { get; set; }
    public string? VisitorName { get; set; }
}