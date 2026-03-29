using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Domain.Data;

public class LocationAggregation
{
    public int OrdersCount { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public int FeedbackCount { get; private set; }
    public int FeedbackSum { get; private set; }
    public int? MinFeedback { get; private set; }

    public void Add(ReportEntry entry)
    {
        OrdersCount++;
        TotalRevenue += entry.TotalRevenue;

        if (entry.CuisineFeedback.HasValue)
        {
            var feedback = entry.CuisineFeedback.Value;
            FeedbackSum += feedback;
            FeedbackCount++;

            if (MinFeedback is null || feedback < MinFeedback)
                MinFeedback = feedback;
        }
    }

    public decimal GetAverageFeedback()
    {
        return FeedbackCount == 0 ? 0m : (decimal)FeedbackSum / FeedbackCount;
    }
}