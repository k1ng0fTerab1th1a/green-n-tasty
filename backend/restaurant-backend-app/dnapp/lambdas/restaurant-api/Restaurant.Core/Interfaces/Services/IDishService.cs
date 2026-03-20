using FluentResults;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IDishService
{
    Task<Result<IReadOnlyList<Dish>>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Dish>>> GetPopularDishesAsync(CancellationToken cancellationToken = default);
}
