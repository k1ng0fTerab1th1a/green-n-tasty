using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces;
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

        public Task<IReadOnlyList<Dish>> GetSpecialityDishesAsync(string locationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationId))
                return Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());

            if (locationId == "672846d5c951184d705b65d7")
            {
                IReadOnlyList<Dish> dishes = new[]
                {
                new Dish("Fresh Strawberry Mint Salad", "$12", "430 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg"),
                new Dish("Avocado Quinoa Bowl", "$14", "520 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg")
            };
                return Task.FromResult(dishes);
            }

            return Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());
        }
    }
}
