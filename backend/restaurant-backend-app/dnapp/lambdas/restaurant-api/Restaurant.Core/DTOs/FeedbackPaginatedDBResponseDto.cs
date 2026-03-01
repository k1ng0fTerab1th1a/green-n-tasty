namespace Restaurant.Core.ServiceDTOs;

public class FeedbackPaginatedDBResponseDto
{
    public int TotalPages { get; set; }
    public int TotalSize { get; set; }
    public List<Feedback> Feedbacks { get; set; } = [];
    public string? NextPageToken { get; set; }
}