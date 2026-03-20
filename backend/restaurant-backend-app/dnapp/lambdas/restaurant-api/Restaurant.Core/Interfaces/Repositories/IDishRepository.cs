using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default);
    Task<Dish?> GetDishByIdAsync(string dishId, CancellationToken ct = default);
}
