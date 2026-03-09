using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IWaiterScheduleRepository
{
    Task<WaiterSchedule?> GetAsync(string tableKey, string date, CancellationToken ct = default);
}
