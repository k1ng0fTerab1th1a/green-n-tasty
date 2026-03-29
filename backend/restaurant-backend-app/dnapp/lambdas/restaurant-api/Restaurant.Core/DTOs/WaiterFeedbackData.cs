namespace Restaurant.Core.DTOs;

public class WaiterFeedbackData
{
    public string WaiterName { get; set; } = string.Empty;
    public string? WaiterImageUrl { get; set; }
    public int WaiterRating { get; set; }
    public int WaiterFeedbacksNumber { get; set; } 
}