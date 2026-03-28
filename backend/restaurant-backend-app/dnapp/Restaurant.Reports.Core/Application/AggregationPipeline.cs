using Restaurant.Reports.Domain.Aggregators;
using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Infrastructure;

namespace Restaurant.Reports.Application;

public class AggregationPipeline
{
    private readonly IReportsRepository _repository;

    public AggregationPipeline(IReportsRepository repository)
    {
        _repository = repository;
    }

    public async Task AggregateAsync(
        DateRange range,
        IEnumerable<IReportAggregator> aggregators,
        CancellationToken ct)
    {
        var aggregatorList = aggregators.ToList();
        var current = range.From.Date;

        while (current <= range.To.Date)
        {
            var dayStart = current;
            var dayEnd = current.AddDays(1).AddTicks(-1);

            var actualFrom = range.From > dayStart ? range.From : dayStart;
            var actualTo = range.To < dayEnd ? range.To : dayEnd;

            await foreach (var entry in _repository.QueryReportsByDateAsync(current, actualFrom, actualTo, ct))
            {
                foreach (var agg in aggregatorList)
                {
                    agg.Process(entry);
                }
            }

            current = current.AddDays(1);
        }
    }
}
