namespace Restaurant.Core.DTOs;

public class CalculatedFeedbackDTO
{
    public string WaiterName { get; set; } = string.Empty;
    public string? WaiterImageUrl { get; set; }
    public double WaiterRating { get; set; }
    public int WaiterFeedbacksNumber { get; set; }
    
    public double CuisineRating { get; set; }
    public int CuisineFeedbacksNumber { get; set; }
}