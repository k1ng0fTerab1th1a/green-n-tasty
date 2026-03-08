namespace Restaurant.Api.Contracts.Requests;

public class UpdateReservationRequest
{
    public string Id { get; set; } = string.Empty;
    public int GuestNumber { get; set; }
    public int TableNumber { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}