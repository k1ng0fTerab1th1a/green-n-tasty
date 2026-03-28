using Restaurant.Core.Models;
using Restaurant.Reports.Domain.Data;
using static Restaurant.Reports.Application.Builders.ReportCalculationHelper;

namespace Restaurant.Reports.Application.Builders;

public class LocationReportBuilder
{
    public List<LocationReportData> Build(
        Dictionary<string, LocationAggregation> current,
        Dictionary<string, LocationAggregation>? previous,
        Dictionary<string, Location>? locations,
        DateTime from,
        DateTime to)
    {
        var result = new List<LocationReportData>();

        foreach (var (locationId, agg) in current)
        {
            LocationAggregation? prev = null;
            Location? location = null;
            previous?.TryGetValue(locationId, out prev);
            locations?.TryGetValue(locationId, out location);

            result.Add(BuildRow(locationId, agg, prev, location, from, to));
        }

        return result;
    }

    private static LocationReportData BuildRow(
        string locationId,
        LocationAggregation current,
        LocationAggregation? previous,
        Location? location,
        DateTime from,
        DateTime to)
    {
        return new LocationReportData
        {
            LocationId = locationId,
            LocationAddress = location?.Address,
            StartDate = from,
            EndDate = to,

            TotalOrders = current.OrdersCount,
            OrderDelta = CalculateDelta(current.OrdersCount, previous?.OrdersCount),

            AvgCuisineRating = current.GetAverageFeedback(),
            MinCuisineRating = current.MinFeedback ?? 0,
            CuisineRatingDelta = CalculateDelta(current.GetAverageFeedback(), previous?.GetAverageFeedback()),

            TotalRevenue = current.TotalRevenue,
            RevenueDelta = CalculateDelta(current.TotalRevenue, previous?.TotalRevenue)
        };
    }
}