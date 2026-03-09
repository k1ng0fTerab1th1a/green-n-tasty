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

    public async Task<IReadOnlyDictionary<string, TableDay>> GetManyByTablesAndDateAsync(
        IEnumerable<string> tableKeys,
        string date,
        CancellationToken ct)
    {
        if (tableKeys == null || !tableKeys.Any() || string.IsNullOrWhiteSpace(date))
        {
            return new Dictionary<string, TableDay>();
        }

        var batchGet = _context.CreateBatchGet<TableDay>();

        foreach (var key in tableKeys)
        {
            batchGet.AddKey(key, date);
        }

        await batchGet.ExecuteAsync(ct);

        return batchGet.Results.ToDictionary(td => td.TableKey);
    }
}
