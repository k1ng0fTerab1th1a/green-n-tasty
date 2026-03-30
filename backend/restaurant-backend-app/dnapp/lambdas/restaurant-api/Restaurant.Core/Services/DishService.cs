using Restaurant.Core.DTOs;
using FluentResults;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class DishService(IDishRepository _dishRepository) : IDishService
{
    public async Task<Result<IReadOnlyList<Dish>>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
        => Result.Ok(await _dishRepository.GetPopularDishesAsync(cancellationToken));

    public async Task<Result<Dish>> GetDishByIdAsync(string dishId, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(dishId, cancellationToken);
        if (dish is null)
            return DishErrors.NotFound;

        return dish;
    }

    public async Task<Result<IReadOnlyList<DishBriefDTO>>> GetMenuBriefDishesAsync(
        string? type,
        string sort,
        CancellationToken cancellationToken = default)
    {
        return Result.Ok(await _dishRepository.GetShortenedDishesAsync(type, sort, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<DishBriefDTO>>> SearchDishesAsync(
        string query,
        string? type,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result.Ok<IReadOnlyList<DishBriefDTO>>(Array.Empty<DishBriefDTO>());

        var safeLimit = limit <= 0 ? 20 : Math.Min(limit, 50);

        return Result.Ok(await _dishRepository.SearchAsync(query, type, safeLimit, cancellationToken));
    }

    public async Task<Result<int>> RebuildSearchIndexAsync(CancellationToken cancellationToken = default)
    {
        return Result.Ok(await _dishRepository.RebuildSearchIndexAsync(cancellationToken));
    }

    public async Task<Result> UpsertDishSearchIndexAsync(string dishId, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(dishId, cancellationToken);
        if (dish is null)
            return DishErrors.NotFound;

        await _dishRepository.UpsertDishSearchIndexAsync(dish, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RemoveDishSearchIndexAsync(string dishId, CancellationToken cancellationToken = default)
    {
        await _dishRepository.RemoveDishSearchIndexAsync(dishId, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<Dish>>> GetSpecialityDishesByLocationIdAsync(
        string locationId,
        CancellationToken cancellationToken = default)
        => Result.Ok(await _dishRepository.GetSpecialityDishesByLocationIdAsync(locationId, cancellationToken));
}
