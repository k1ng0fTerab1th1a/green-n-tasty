using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Domain.Aggregators;

public class WaiterAggregator : IReportAggregator
{
    private readonly Dictionary<string, WaiterAggregation> _data = new();

    public void Process(ReportEntry entry)
    {
        if (!_data.TryGetValue(entry.WaiterId, out var agg))
        {
            agg = new WaiterAggregation();
            _data[entry.WaiterId] = agg;
        }

        agg.Add(entry);
    }

    public Dictionary<string, WaiterAggregation> GetResult() => _data;
}
