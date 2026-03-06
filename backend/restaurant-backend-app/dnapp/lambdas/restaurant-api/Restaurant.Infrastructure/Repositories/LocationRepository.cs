using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories
{
    public sealed class LocationRepository : ILocationRepository
    {
        private const string EntityTypeIndexName = "entityType-index";
        private const string LocationEntityType = "LOCATION";

        private readonly IDynamoDBContext _context;

        public LocationRepository(IDynamoDBContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
        {
            var op = new DynamoDBOperationConfig { IndexName = EntityTypeIndexName };

            var search = _context.QueryAsync<Location>(LocationEntityType, op);
            var items = await search.GetRemainingAsync();

            return items;
        }

        public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
        {
            return GetLocationsAsync(cancellationToken);
        }

        public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _context.LoadAsync<Location>(id, cancellationToken);
        }
    }
}
