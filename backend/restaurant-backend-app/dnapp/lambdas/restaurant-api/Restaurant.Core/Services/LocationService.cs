using FluentResults;
using Restaurant.Core.Errors;
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

    public async Task<Result<IReadOnlyList<Location>>> GetLocationsAsync(CancellationToken cancellationToken = default)
        => Result.Ok(await _repository.GetLocationsAsync(cancellationToken));

    public async Task<Result<IReadOnlyList<Location>>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
        => Result.Ok(await _repository.GetLocationOptionsAsync(cancellationToken));

    public async Task<Result<Location>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var location = await _repository.GetByIdAsync(id, cancellationToken);
        if (location is null) return LocationErrors.NotFound;
        return location;
    }
}
