using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface ITableService
{
    Task<Result<IList<TableWithAvailableSlots>>> GetAvailableTablesAsync(
        GetAvailableTablesQuery query,
        CancellationToken ct);
}
