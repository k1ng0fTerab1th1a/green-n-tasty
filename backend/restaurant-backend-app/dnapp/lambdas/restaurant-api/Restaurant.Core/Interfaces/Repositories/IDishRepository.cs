using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken);

    Task<List<Dish>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct);

    Task<Dish?> GetDishByIdAsync(string dishId, CancellationToken ct);

    Task<IReadOnlyList<DishBriefDTO>> GetShortenedDishesAsync(string? type, string sort, CancellationToken ct);

    Task<IReadOnlyList<DishBriefDTO>> SearchAsync(string query, string? type, int limit, CancellationToken ct);

    Task<int> RebuildSearchIndexAsync(CancellationToken ct);

    Task UpsertDishSearchIndexAsync(Dish dish, CancellationToken ct);

    Task RemoveDishSearchIndexAsync(string dishId, CancellationToken ct);
}
