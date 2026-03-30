using FluentResults;
using Restaurant.Reports.Domain.Data;

namespace Restaurant.Reports.Application;

public interface IReportService
{
	Task<Result<List<LocationReportData>>> GetLocationReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct);
	Task<Result<List<WaiterReportData>>> GetWaiterReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct);
	Task<Result<FullReportData>> GetFullReportDataAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct);
	Task<Result<byte[]>> ExportLocationReportAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct);
	Task<Result<byte[]>> ExportWaiterReportAsync(DateRange range, DateRange? comparisonRange, CancellationToken ct);
}
