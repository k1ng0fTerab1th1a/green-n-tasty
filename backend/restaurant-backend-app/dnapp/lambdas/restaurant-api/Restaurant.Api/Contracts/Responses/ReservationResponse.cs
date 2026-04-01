namespace Restaurant.Api.Contracts.Responses;

public sealed class ReservationResponse
{
    public string Id { get; set; } = null!;
    public string LocationId { get; set; } = null!;
    public string LocationAddress { get; set; } = null!;
    public string? CustomerName { get; set; } = null!;
    public string WaiterName { get; set; } = null!;
    public bool IsMealServed { get; set; }
    public int DishCount { get; set; }
    public int TableNumber { get; set; }
    public int GuestsCount { get; set; }
    public string StartDateTime { get; set; } = null!;
    public string EndDateTime { get; set; } = null!;
    public string? ActualStartTime { get; set; }
    public string? ActualEndTime { get; set; }
    public string Status { get; set; } = null!;
    public bool IsCreatedByWaiter { get; set; }
    public string? VisitorName { get; set; }
}
