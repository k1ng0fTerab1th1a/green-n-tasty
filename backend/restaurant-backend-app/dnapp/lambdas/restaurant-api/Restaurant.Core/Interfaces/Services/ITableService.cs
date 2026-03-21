using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface ITableService
{
    Task<Result<IList<TableWithAvailableSlots>>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly? time,
        string? locationId,
        int? capacity,
        CancellationToken ct);
}
