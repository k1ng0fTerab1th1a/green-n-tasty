using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken);
    Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken);
}
