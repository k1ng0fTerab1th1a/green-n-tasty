using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IDynamoDBContext _context) : IDishRepository
{
    public async Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = "PopularDishesIndex"
        };

        var asyncSearch = _context.QueryAsync<Dish>("true", config);

        var results = await asyncSearch.GetRemainingAsync(cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken cancellationToken)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = "SpecialityIndex"
        };

        var asyncSearch = _context.QueryAsync<Dish>(locationId, config);

        var results = await asyncSearch.GetRemainingAsync(cancellationToken);

        return results;
    }
    
    public async Task<Dish?> GetDishByIdAsync(string dishId, CancellationToken ct = default)
    {
        return await _context.LoadAsync<Dish>(dishId, ct);
    }
}
