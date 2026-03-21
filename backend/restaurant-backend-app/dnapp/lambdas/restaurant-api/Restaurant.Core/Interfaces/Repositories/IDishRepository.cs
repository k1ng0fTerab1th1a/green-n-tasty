using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default);

    Task<List<Dish>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
}
