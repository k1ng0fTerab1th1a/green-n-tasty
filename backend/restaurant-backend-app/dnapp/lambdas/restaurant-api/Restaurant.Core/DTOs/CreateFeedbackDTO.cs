namespace Restaurant.Core.DTOs;

public class CreateFeedbackDTO
{
    public string ReservationId { get; set; } = string.Empty;
    
    public int? ServiceRating { get; set; }
    public string? ServiceComment { get; set; }
    
    public int? CuisineRating { get; set; }
    public string? CuisineComment { get; set; }
}