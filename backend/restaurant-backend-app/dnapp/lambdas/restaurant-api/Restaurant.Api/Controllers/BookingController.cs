using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[Route("bookings")]
[ApiController]
public class BookingController(ITableService _tableService) : ControllerBase
{
    [HttpGet("tables")]
    [ProducesResponseType(typeof(ApiResponse<IList<TableWithAvailableSlots>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IList<TableWithAvailableSlots>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<IList<TableWithAvailableSlots>>> GetAvailableTables(
        [FromQuery] string date,
        [FromQuery] string? time,
        [FromQuery] string? locationId,
        [FromQuery] int? guests,
        [FromQuery] string? excludeReservationId,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
        {
            return ApiResponse<IList<TableWithAvailableSlots>>.Fail(StatusCodes.Status400BadRequest, "Date must be in yyyy-MM-dd format.");
        }

        TimeOnly? parsedTime = null;
        if (!string.IsNullOrWhiteSpace(time))
        {
            if (TimeOnly.TryParseExact(time, "HH:mm", out var t))
            {
                parsedTime = t;
            }
        }

        var availableTables = await _tableService.GetAvailableTablesAsync(parsedDate, parsedTime, locationId, guests, excludeReservationId, ct);

        if (availableTables.IsFailed)
        {
            return availableTables.Errors[0].ToApiResponse<IList<TableWithAvailableSlots>>();
        }
        return ApiResponse<IList<TableWithAvailableSlots>>.Success(StatusCodes.Status200OK, availableTables.Value);
    }
}
