using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class TableDayRepository(IDynamoDBContext _context) : ITableDayRepository
{
    public async Task<TableDay?> GetByTableAndDateAsync(string tableId, string date, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableId) || string.IsNullOrWhiteSpace(date))
            return null;

        var tableDay = await _context.LoadAsync<TableDay>(tableId, date, ct);

        return tableDay;
    }
}
