using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repository;

public class WaiterListRepository : IWaiterListRepository
{
    private readonly IDynamoDBContext _context;

    public WaiterListRepository(IDynamoDBContext context)
    {
        _context = context;
    
    }
    public async Task<bool> ContainsAsync(string email)
    {
        return await _context.LoadAsync<WaiterListEntry?>(email) != null;
    }
}
