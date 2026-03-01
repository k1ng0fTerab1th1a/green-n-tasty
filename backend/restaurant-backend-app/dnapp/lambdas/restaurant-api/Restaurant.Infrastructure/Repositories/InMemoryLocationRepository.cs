using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories
{
    public sealed class InMemoryLocationRepository : ILocationRepository
    {
        private static readonly IReadOnlyList<Location> LocationsSeed = new List<Location>
    {
        new Location
        {
            Id = "672846d5c951184d705b65d7",
            Address = "123 Main St",
            Description = "Downtown location with a bright open kitchen.",
            TotalCapacity = 120,
            AverageOccupancy = 0.70,
            ImageUrl = "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg",
            Rating = 4.8
        },
        new Location
        {
            Id = "582846d5c951184d705b65d1",
            Address = "45 Riverside Ave",
            Description = "Family-friendly spot near the river.",
            TotalCapacity = 90,
            AverageOccupancy = 0.61,
            ImageUrl = "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg",
            Rating = 4.6
        },
        new Location
        {
            Id = "a5d95674-e4aa-43be-8d50-7f26bcc17da0",
            Address = "9 Old Town Square",
            Description = "Cozy historic venue with seasonal menu.",
            TotalCapacity = 60,
            AverageOccupancy = 0.78,
            ImageUrl = "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg",
            Rating = 4.9
        }
    };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<Dish>> SpecialityDishesSeed
            = new Dictionary<string, IReadOnlyList<Dish>>
            {
                ["672846d5c951184d705b65d7"] = new List<Dish>
                {
                new Dish("Fresh Strawberry Mint Salad", "$12", "430 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg"),
                new Dish("Avocado Quinoa Bowl", "$14", "520 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg")
                },
                ["582846d5c951184d705b65d1"] = new List<Dish>
                {
                new Dish("Grilled Veggie Plate", "$16", "600 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg")
                },
                ["a5d95674-e4aa-43be-8d50-7f26bcc17da0"] = new List<Dish>
                {
                new Dish("Pumpkin Cream Soup", "$9", "350 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg"),
                new Dish("Berry Cheesecake", "$8", "160 g",
                    "https://green-and-tasty.s3.eu-central-1.amazonaws.com/img/ff7863bf-63eb-4f2e-8041-75a81507acff.jpg")
                }
            };

        public Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(LocationsSeed);

        public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(LocationsSeed);

        public Task<IReadOnlyList<Dish>> GetSpecialityDishesAsync(string locationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationId))
                return Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());

            return Task.FromResult(SpecialityDishesSeed.TryGetValue(locationId, out var dishes)
                ? dishes
                : Array.Empty<Dish>());
        }
    }
}
