using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Api;

public class MockTableDayRepository : ITableDayRepository
{
    public Task<TableDay?> GetByTableAndDateAsync(string tableKey, string date, CancellationToken ct)
    {
        if (tableKey.Equals("d3e31fb113f6458aa96781a4103cc3ba#3") && date.Equals("2026-03-07"))
        {
            return Task.FromResult<TableDay?>(new TableDay
            {
                TableKey = "d3e31fb113f6458aa96781a4103cc3ba#3",
                Date = "2026-03-07",
                ReservedSlots = new HashSet<string> { "2026-03-07T14:15:00Z", "2026-03-07T14:30:00Z", "2026-03-07T14:45:00Z", "2026-03-07T15:00:00Z", "2026-03-07T15:15:00Z" },
                Ttl = 1772974800
            });
        }

        return Task.FromResult<TableDay?>(null);
    }
}

public class MockTableRepository : ITableRepository
{
    public Task<IReadOnlyList<Table>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<Table>>([
            new Table {
                LocationId = "d3e31fb113f6458aa96781a4103cc3ba",
                TableNumber = 3,
                Capacity = 4,
                LocationAddress = "Main Street 1"
            }
        ]);
    }

    public Task<IReadOnlyList<Table>> GetByLocationIdAsync(string locationId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
