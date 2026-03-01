namespace Restaurant.Core.ServiceDTOs;

public class FeedbackPaginatedDto
{
    public int TotalPages { get; set; }
    public int TotalElements { get; set; }
    public int Size { get; set; }
    public List<FeedbackDTO> Content { get; set; } = [];
    public string? NextPageToken { get; set; }
    public int Number { get; set; }
}