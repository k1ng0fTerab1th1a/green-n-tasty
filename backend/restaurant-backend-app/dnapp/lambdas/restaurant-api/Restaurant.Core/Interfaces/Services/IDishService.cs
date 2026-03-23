using Restaurant.Core.DTOs;
using FluentResults;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IDishService
{
    Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default);
    Task<Dish?> GetDishByIdAsync(string dishId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DishBriefDTO>> GetMenuBriefDishesAsync(string? type, string sort, CancellationToken 
        cancellationToken = default);
}
