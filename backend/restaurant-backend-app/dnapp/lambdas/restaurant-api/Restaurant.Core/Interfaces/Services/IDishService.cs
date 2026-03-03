using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services
{
    public interface IDishService
    {
        Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken);
        Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default);
    }
}
