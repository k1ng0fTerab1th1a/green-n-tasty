using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[Route("bookings")]
[ApiController]
[Authorize]
public class BookingController(ITableService _tableService) : ControllerBase
{
    [HttpGet("tables")]
    public async Task<ApiResponse<IList<TableWithAvailableSlots>>> GetAvailableTables(
        [FromQuery] string date, 
        [FromQuery] string? time,
        [FromQuery] string? locationId,
        [FromQuery] int? guests,
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

        var availableTables = await _tableService.GetAvailableTablesAsync(parsedDate, parsedTime, locationId, guests, ct);
        return ApiResponse<IList<TableWithAvailableSlots>>.Success(StatusCodes.Status200OK, availableTables);
    }
}
