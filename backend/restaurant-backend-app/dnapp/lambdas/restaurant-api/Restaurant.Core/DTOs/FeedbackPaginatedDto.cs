namespace Restaurant.Core.DTOs;

public class FeedbackPaginatedDto
{
    public int Size { get; set; }
    public List<FeedbackDTO> Content { get; set; } = [];
    public string? NextPageToken { get; set; }
}