using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public sealed class LocationService : ILocationService
{
    private readonly ILocationRepository _repository;

    public LocationService(ILocationRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
        => _repository.GetLocationsAsync(cancellationToken);

    public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
        => _repository.GetLocationOptionsAsync(cancellationToken);
}
