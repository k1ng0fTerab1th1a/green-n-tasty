using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class WaiterListRepository : IWaiterListRepository
{
    private readonly IDynamoDBContext _context;

    public WaiterListRepository(IDynamoDBContext context)
    {
        _context = context;

    }
    public async Task<bool> ContainsAsync(string email, CancellationToken ct = default)
    {
        return await _context.LoadAsync<WaiterListEntry?>(email, ct) != null;
    }

    public async Task<WaiterListEntry?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.LoadAsync<WaiterListEntry?>(email, ct);
    }
}
