using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Domain.Aggregators;

public class LocationAggregator : IReportAggregator
{
    private readonly Dictionary<string, LocationAggregation> _data = new();

    public void Process(ReportEntry entry)
    {
        if (!_data.TryGetValue(entry.LocationId, out var agg))
        {
            agg = new LocationAggregation();
            _data[entry.LocationId] = agg;
        }

        agg.Add(entry);
    }

    public Dictionary<string, LocationAggregation> GetResult() => _data;
}