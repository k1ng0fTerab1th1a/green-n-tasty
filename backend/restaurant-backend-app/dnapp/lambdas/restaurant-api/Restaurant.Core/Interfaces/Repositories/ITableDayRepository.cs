using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface ITableDayRepository
{
    Task<TableDay?> GetByTableAndDateAsync(string tableKey, string date, CancellationToken ct);
}
