using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface ILocationService
{
    Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default);
    Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
