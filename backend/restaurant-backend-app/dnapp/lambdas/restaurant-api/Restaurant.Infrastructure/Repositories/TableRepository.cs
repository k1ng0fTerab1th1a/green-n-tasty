using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;

namespace Restaurant.Infrastructure.Repositories;

public class TableRepository : ITableRepository
{
    private readonly IDynamoDBContext _context;

    public TableRepository(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Table>> GetAllAsync(CancellationToken ct)
    {
        var conditions = new List<ScanCondition>();
        var search = _context.ScanAsync<Table>(conditions);

        return await search.GetRemainingAsync(ct);
    }

    public async Task<IReadOnlyList<Table>> GetByLocationIdAsync(string locationId, CancellationToken ct)
    {
        var search = _context.QueryAsync<Table>(locationId);

        return await search.GetRemainingAsync(ct);
    }
}
