using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class DishService(IDishRepository _dishRepository) : IDishService
{
    public Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
    {
        return _dishRepository.GetPopularDishesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken = default)
    {
        return await _dishRepository.GetSpecialityDishesByLocationIdAsync(locationId, cancellationToken);
    }
}