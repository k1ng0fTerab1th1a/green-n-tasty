namespace Restaurant.Api.Models;

public class PaginatedResult<T>
{
    public IEnumerable<T> Content { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int Number { get; set; } // I hope "Number" in openapi classification means the "PageNumber"
    public string? NextPageToken { get; set; } // Needed for pagination
}