using FluentResults;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class DishService(IDishRepository _dishRepository) : IDishService
{
    public async Task<Result<IReadOnlyList<Dish>>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
        => Result.Ok(await _dishRepository.GetPopularDishesAsync(cancellationToken));

    public async Task<Result<IReadOnlyList<Dish>>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken = default)
        => Result.Ok(await _dishRepository.GetSpecialityDishesByLocationIdAsync(locationId, cancellationToken));
}