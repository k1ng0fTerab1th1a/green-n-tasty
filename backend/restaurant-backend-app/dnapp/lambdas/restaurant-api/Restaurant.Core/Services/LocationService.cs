using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services
{
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

        public Task<IReadOnlyList<Dish>> GetSpecialityDishesAsync(string locationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationId))
                return Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());

            return _repository.GetSpecialityDishesAsync(locationId, cancellationToken);
        }
    }
}
