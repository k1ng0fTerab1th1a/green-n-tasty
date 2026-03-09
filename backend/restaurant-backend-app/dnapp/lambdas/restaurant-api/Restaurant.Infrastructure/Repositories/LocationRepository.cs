using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{

    private readonly IDynamoDBContext _context;

    public LocationRepository(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var search = _context.ScanAsync<Location>(new List<ScanCondition>());
        return await search.GetRemainingAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
    {
        return GetLocationsAsync(cancellationToken);
    }

    public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _context.LoadAsync<Location?>(id, cancellationToken);
    }
}
