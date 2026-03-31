namespace Restaurant.Reports.Domain.Data;

public class WaiterReportData
{
    public string? LocationAddress { get; init; }
    public string WaiterId { get; init; }
    public string WaiterName { get; init; }
    public string WaiterEmail { get; init; }

    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public int WaiterWorkingHours { get; init; }
    public int OrdersProcessed { get; init; }
    public decimal? OrdersProcessedDelta { get; init; }

    public decimal AvgServiceRating { get; init; }
    public int MinServiceRating { get; init; }
    public decimal? AvgServiceRatingDelta { get; init; }
}
