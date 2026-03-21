namespace Restaurant.Api.Contracts.Requests;

public sealed class CreateReservationForWaiterRequest
{
    public string LocationId { get; set; } = null!;
    public int TableNumber { get; set; }
    public string Date { get; set; } = null!;
    public string TimeFrom { get; set; } = null!;
    public string TimeTo { get; set; } = null!;
    public int GuestsCount { get; set; }
    public string? CustomerId { get; set; }
    public string? VisitorName { get; set; }
}