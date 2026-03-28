using Restaurant.Core.Models;
using Restaurant.Reports.Domain.Data;
using static Restaurant.Reports.Application.Builders.ReportCalculationHelper;

namespace Restaurant.Reports.Application.Builders;

public class WaiterReportBuilder
{
    public List<WaiterReportData> Build(
        Dictionary<string, WaiterAggregation> current,
        Dictionary<string, WaiterAggregation>? previous,
        Dictionary<string, User>? waiters,
        Dictionary<string, Location>? locations,
        Dictionary<string, int>? waiterShiftCounts,
        DateTime from,
        DateTime to)
    {
        var result = new List<WaiterReportData>();

        foreach (var (waiterId, agg) in current)
        {
            WaiterAggregation? prev = null;
            User? waiter = null;
            Location? location = null;

            previous?.TryGetValue(waiterId, out prev);
            waiters?.TryGetValue(waiterId, out waiter);

            if (agg.LocationId is not null)
                locations?.TryGetValue(agg.LocationId, out location);

            int shiftCount = 0;
            waiterShiftCounts?.TryGetValue(waiterId, out shiftCount);

            result.Add(BuildRow(waiterId, agg, prev, waiter, location, shiftCount, from, to));
        }

        return result;
    }

    private static WaiterReportData BuildRow(
        string waiterId,
        WaiterAggregation current,
        WaiterAggregation? previous,
        User? waiter,
        Location? location,
        int shiftCount,
        DateTime from,
        DateTime to)
    {
        var workingHours = CalculateWorkingHours(location, shiftCount);

        return new WaiterReportData
        {
            WaiterId = waiterId,
            WaiterName = waiter is not null ? $"{waiter.FirstName} {waiter.LastName}" : waiterId,
            WaiterEmail = waiter?.Email ?? "",
            LocationAddress = location?.Address,
            StartDate = from,
            EndDate = to,

            WaiterWorkingHours = workingHours,
            OrdersProcessed = current.OrdersCount,
            OrdersProcessedDelta = CalculateDelta(current.OrdersCount, previous?.OrdersCount),

            AvgServiceRating = current.GetAverageFeedback(),
            MinServiceRating = current.MinFeedback ?? 0,
            AvgServiceRatingDelta = CalculateDelta(current.GetAverageFeedback(), previous?.GetAverageFeedback())
        };
    }

    private static int CalculateWorkingHours(Location? location, int shiftCount)
    {
        if (location is null || shiftCount <= 0)
            return 0;

        if (TimeSpan.TryParse(location.OpenTime, out var open)
            && TimeSpan.TryParse(location.CloseTime, out var close)
            && close > open)
        {
            return (int)((close - open).TotalHours * shiftCount);
        }

        return 0;
    }
}