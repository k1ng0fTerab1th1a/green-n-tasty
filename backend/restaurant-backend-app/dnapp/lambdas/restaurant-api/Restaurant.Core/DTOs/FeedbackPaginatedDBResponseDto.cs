namespace Restaurant.Core.ServiceDTOs;

public class FeedbackPaginatedDBResponseDto
{
    public List<Feedback> Feedbacks { get; set; } = [];
    public string? NextPageToken { get; set; }
}