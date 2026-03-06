using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface ITableService
{
    Task<IReadOnlyList<TableWithAvailableSlots>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly? time,
        string? locationId,
        int? capacity,
        CancellationToken ct);
}
