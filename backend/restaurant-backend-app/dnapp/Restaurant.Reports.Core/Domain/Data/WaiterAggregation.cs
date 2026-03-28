using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Domain.Data;

public class WaiterAggregation
{
    public string? LocationId { get; private set; }
    public int OrdersCount { get; private set; }
    public int FeedbackCount { get; private set; }
    public int FeedbackSum { get; private set; }
    public int? MinFeedback { get; private set; }

    public void Add(ReportEntry entry)
    {
        LocationId ??= entry.LocationId;
        OrdersCount++;

        if (entry.ServiceFeedback.HasValue)
        {
            var feedback = entry.ServiceFeedback.Value;
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