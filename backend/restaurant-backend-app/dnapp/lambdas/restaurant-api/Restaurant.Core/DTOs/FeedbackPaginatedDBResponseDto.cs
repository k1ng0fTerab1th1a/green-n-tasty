using Restaurant.Core.Models;

namespace Restaurant.Core.DTOs;

public class FeedbackPaginatedDBResponseDto
{
    public List<Feedback> Feedbacks { get; set; } = [];
    public string? NextPageToken { get; set; }
}