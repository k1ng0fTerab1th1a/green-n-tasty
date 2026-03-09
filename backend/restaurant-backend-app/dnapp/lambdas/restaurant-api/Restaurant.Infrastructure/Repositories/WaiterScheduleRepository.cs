using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public sealed class WaiterScheduleRepository : IWaiterScheduleRepository
{
    private readonly IDynamoDBContext _context;

    public WaiterScheduleRepository(IDynamoDBContext context) => _context = context;

    public async Task<WaiterSchedule?> GetAsync(string tableKey, string date, CancellationToken ct = default)
        => await _context.LoadAsync<WaiterSchedule>(tableKey, date, ct);
}
