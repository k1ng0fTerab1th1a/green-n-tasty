using FluentResults;
using Restaurant.Core.Models;
using Restaurant.Reports.Application.Builders;
using Restaurant.Reports.Domain.Aggregators;
using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Domain.Errors;
using Restaurant.Reports.Infrastructure;

namespace Restaurant.Reports.Application;

public class ReportService
{
    private readonly IReportsRepository _repository;
    private readonly AggregationPipeline _pipeline;
    private readonly LocationReportBuilder _locationBuilder = new();
    private readonly WaiterReportBuilder _waiterBuilder = new();

    public ReportService(IReportsRepository repository)
    {
        _repository = repository;
        _pipeline = new AggregationPipeline(repository);
    }

    public async Task<Result<List<LocationReportData>>> GetLocationReportAsync(
        DateRange range,
        DateRange? comparisonRange,
        CancellationToken ct)
    {
        if (range.From > range.To)
            return ReportErrors.InvalidDateRange;

        if (comparisonRange is not null && comparisonRange.From > comparisonRange.To)
            return ReportErrors.InvalidDateRange;

        var currentAgg = new LocationAggregator();
        LocationAggregator? comparisonAgg = null;

        if (comparisonRange is not null)
        {
            comparisonAgg = new LocationAggregator();
            await _pipeline.AggregateAsync(comparisonRange, [comparisonAgg], ct);
        }

        await _pipeline.AggregateAsync(range, [currentAgg], ct);

        var locations = await _repository.GetAllLocationsAsync(ct);

        return _locationBuilder.Build(
            currentAgg.GetResult(),
            comparisonAgg?.GetResult(),
            locations,
            range.From,
            range.To);
    }

    public async Task<Result<FullReportData>> GetFullReportAsync(
        DateRange range,
        DateRange? comparisonRange,
        CancellationToken ct)
    {
        if (range.From > range.To)
            return ReportErrors.InvalidDateRange;

        if (comparisonRange is not null && comparisonRange.From > comparisonRange.To)
            return ReportErrors.InvalidDateRange;

        var locationAgg = new LocationAggregator();
        var waiterAgg = new WaiterAggregator();
        LocationAggregator? comparisonLocationAgg = null;
        WaiterAggregator? comparisonWaiterAgg = null;

        if (comparisonRange is not null)
        {
            comparisonLocationAgg = new LocationAggregator();
            comparisonWaiterAgg = new WaiterAggregator();
            await _pipeline.AggregateAsync(comparisonRange, [comparisonLocationAgg, comparisonWaiterAgg], ct);
        }

        await _pipeline.AggregateAsync(range, [locationAgg, waiterAgg], ct);

        var locations = await _repository.GetAllLocationsAsync(ct);
        var waiters = await _repository.GetAllWaitersAsync(ct);
        var shiftCounts = await _repository.GetWaiterShiftCountsAsync(range.From, range.To, ct);

        var locationReport = _locationBuilder.Build(
            locationAgg.GetResult(),
            comparisonLocationAgg?.GetResult(),
            locations,
            range.From,
            range.To);

        var waiterReport = _waiterBuilder.Build(
            waiterAgg.GetResult(),
            comparisonWaiterAgg?.GetResult(),
            waiters,
            locations,
            shiftCounts,
            range.From,
            range.To);

        return new FullReportData(locationReport, waiterReport);
    }
}
