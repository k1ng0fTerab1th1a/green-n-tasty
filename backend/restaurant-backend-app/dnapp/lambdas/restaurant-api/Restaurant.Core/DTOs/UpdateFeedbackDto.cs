namespace Restaurant.Core.DTOs;

public class UpdateFeedbackDto : CreateFeedbackDTO
{
    public string? ServiceId { get; set; }
    
    public string? KitchenId { get; set; }
}