using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default);
    Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateUserRatingAsync(string locationId, double newFeedbackRating, CancellationToken ct = default);
}
