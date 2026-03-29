namespace Restaurant.Reports.Domain.Data;

public class LocationReportData
{
    public required string LocationId { get; init; }
    public string? LocationAddress { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public int TotalOrders { get; init; }
    public decimal? OrderDelta { get; init; }

    public decimal AvgCuisineRating { get; init; }
    public int MinCuisineRating { get; init; }
    public decimal? CuisineRatingDelta { get; init; }

    public decimal TotalRevenue { get; init; }
    public decimal? RevenueDelta { get; init; }
}
