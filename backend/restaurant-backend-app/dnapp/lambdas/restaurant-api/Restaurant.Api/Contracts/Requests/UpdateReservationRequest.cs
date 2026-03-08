namespace Restaurant.Api.Contracts.Requests;

public class UpdateReservationRequest
{
    public string Id { get; set; } = string.Empty;
    public int GuestNumber { get; set; }
    public int TableNumber { get; set; }
    public string Date { get; set; } = string.Empty;
    public string TimeFrom { get; set; } = string.Empty;
    public string TimeTo { get; set; } = string.Empty;
}