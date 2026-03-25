using Restaurant.Core.DTOs;
using FluentResults;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IDishService
{
    Task<Result<IReadOnlyList<Dish>>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<Dish>>> GetPopularDishesAsync(CancellationToken cancellationToken);
    Task<Result<Dish>> GetDishByIdAsync(string dishId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<DishBriefDTO>>> GetMenuBriefDishesAsync(string? type, string sort, CancellationToken cancellationToken);
}
