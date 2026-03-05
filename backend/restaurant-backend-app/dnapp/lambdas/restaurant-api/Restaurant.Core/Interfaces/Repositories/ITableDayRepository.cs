using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface ITableDayRepository
{
    Task<TableDay?> GetByTableAndDateAsync(string tableId, string date, CancellationToken ct);
}
