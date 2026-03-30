namespace Restaurant.Core.DTOs;

public class FeedbackOfUserDTO
{
    public string? ServiceFeedbackId { get; set; }
    public string? ServiceComment { get; set; }
    public int? ServiceRating { get; set; }
    
    public string? KitchenFeedbackId { get; set; }
    public string? KitchenComment { get; set; }
    public int? KitchenRating { get; set; }
}