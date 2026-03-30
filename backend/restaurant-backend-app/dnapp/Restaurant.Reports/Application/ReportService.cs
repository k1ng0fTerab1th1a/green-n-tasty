using DocumentFormat.OpenXml.Drawing.Diagrams;
using ClosedXML.Excel;
using FluentResults;
using Restaurant.Core.Models;
using Restaurant.Reports.Application.Builders;
using Restaurant.Reports.Domain.Aggregators;
using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Domain.Errors;
using Restaurant.Reports.Application.Exporters;
using Restaurant.Reports.Infrastructure;

namespace Restaurant.Reports.Application;

public class ReportService : IReportService
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

    public async Task<Result<List<LocationReportData>>> GetLocationReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct)
    {
        if (range.From > range.To)
            return ReportErrors.InvalidDateRange;

        if (comparisonRange is not null && comparisonRange.From > comparisonRange.To)
            return ReportErrors.InvalidDateRange;

        var locationAgg = new LocationAggregator();
        LocationAggregator? comparisonLocationAgg = null;
        if (comparisonRange is not null)
        {
            comparisonLocationAgg = new LocationAggregator();
            await _pipeline.AggregateAsync(comparisonRange, [comparisonLocationAgg], ct);
        }

        await _pipeline.AggregateAsync(range, [locationAgg], ct);

        var locations = await _repository.GetAllLocationsAsync(ct);

        var locationReportData = _locationBuilder.Build(
            locationAgg.GetResult(),
            comparisonLocationAgg?.GetResult(),
            locations,
            range.From,
            range.To);

        return locationReportData;
    }

    public async Task<Result<List<WaiterReportData>>> GetWaiterReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct)
    {
        if (range.From > range.To)
            return ReportErrors.InvalidDateRange;

        if (comparisonRange is not null && comparisonRange.From > comparisonRange.To)
            return ReportErrors.InvalidDateRange;

        var waiterAgg = new WaiterAggregator();
        WaiterAggregator? comparisonWaiterAgg = null;
        if (comparisonRange is not null)
        {
            comparisonWaiterAgg = new WaiterAggregator();
            await _pipeline.AggregateAsync(comparisonRange, [comparisonWaiterAgg], ct);
        }

        await _pipeline.AggregateAsync(range, [waiterAgg], ct);

        var waiters = await _repository.GetAllWaitersAsync(ct);
        var locations = await _repository.GetAllLocationsAsync(ct);
        var shiftCounts = await _repository.GetWaiterShiftCountsAsync(range.From, range.To, ct);

        var waiterReportData = _waiterBuilder.Build(
            waiterAgg.GetResult(),
            comparisonWaiterAgg?.GetResult(),
            waiters,
            locations,
            shiftCounts,
            range.From,
            range.To);

        return waiterReportData;
    }

    public async Task<Result<FullReportData>> GetFullReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct)
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

        var locationReportData = _locationBuilder.Build(
            locationAgg.GetResult(),
            comparisonLocationAgg?.GetResult(),
            locations,
            range.From,
            range.To);

        var waiterReportData = _waiterBuilder.Build(
            waiterAgg.GetResult(),
            comparisonWaiterAgg?.GetResult(),
            waiters,
            locations,
            shiftCounts,
            range.From,
            range.To);

        return new FullReportData(locationReportData, waiterReportData);
    }

    public async Task<Result<byte[]>> ExportLocationReportAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct)
    {
        var result = await GetLocationReportDataAsync(range, comparisonRange, ct);
        if (result.IsFailed) return result.ToResult();

        using var workbook = new XLWorkbook();
        ExcelReportExporter.AddLocationSheet(workbook, result.Value);
        return SaveWorkbook(workbook);
    }

    public async Task<Result<byte[]>> ExportWaiterReportAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct)
    {
        var result = await GetWaiterReportDataAsync(range, comparisonRange, ct);
        if (result.IsFailed) return result.ToResult();

        using var workbook = new XLWorkbook();
        ExcelReportExporter.AddWaiterSheet(workbook, result.Value);
        return SaveWorkbook(workbook);
    }

    private static byte[] SaveWorkbook(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
