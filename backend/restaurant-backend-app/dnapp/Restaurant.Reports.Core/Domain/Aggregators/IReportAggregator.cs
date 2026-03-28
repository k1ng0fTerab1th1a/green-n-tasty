using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Domain.Aggregators;

public interface IReportAggregator
{
    void Process(ReportEntry entry);
}