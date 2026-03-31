using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Reports.Application;
using Restaurant.Reports.Domain.Data;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("reports")]
[Authorize(Roles = "ADMIN")]
public class ReportController(IReportService reportService) : ControllerBase
{
    [HttpGet("locations")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationReportData>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<LocationReportData>>> GetLocationReport([FromQuery] ReportRequest request, CancellationToken ct)
    {
        var range = new DateRange(request.From, request.To);
        var comparison = BuildComparisonRange(request);

        var result = await reportService.GetLocationReportDataAsync(range, comparison, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<LocationReportData>>();

        return ApiResponse<List<LocationReportData>>.Success(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet("waiters")]
    [ProducesResponseType(typeof(ApiResponse<List<WaiterReportData>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<WaiterReportData>>> GetWaiterReport([FromQuery] ReportRequest request, CancellationToken ct)
    {
        var range = new DateRange(request.From, request.To);
        var comparison = BuildComparisonRange(request);

        var result = await reportService.GetWaiterReportDataAsync(range, comparison, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<WaiterReportData>>();

        return ApiResponse<List<WaiterReportData>>.Success(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet("locations/export")]
    public async Task<IActionResult> ExportLocationReport([FromQuery] ReportRequest request, CancellationToken ct)
    {
        var range = new DateRange(request.From, request.To);
        var comparison = BuildComparisonRange(request);

        var result = await reportService.ExportLocationReportAsync(range, comparison, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"location-report-{request.From:yyyy-MM-dd}_{request.To:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("waiters/export")]
    public async Task<IActionResult> ExportWaiterReport([FromQuery] ReportRequest request, CancellationToken ct)
    {
        var range = new DateRange(request.From, request.To);
        var comparison = BuildComparisonRange(request);

        var result = await reportService.ExportWaiterReportAsync(range, comparison, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"waiter-report-{request.From:yyyy-MM-dd}_{request.To:yyyy-MM-dd}.xlsx");
    }

    private static DateRange? BuildComparisonRange(ReportRequest request)
    {
        if (request.CompareFrom.HasValue && request.CompareTo.HasValue)
            return new DateRange(request.CompareFrom.Value, request.CompareTo.Value);

        return null;
    }
}