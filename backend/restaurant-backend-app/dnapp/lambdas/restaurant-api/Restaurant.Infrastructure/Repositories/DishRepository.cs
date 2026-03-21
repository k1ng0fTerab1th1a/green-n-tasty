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

    public async Task<List<Dish>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var batch = _context.CreateBatchGet<Dish>();
        foreach (var id in ids)
            batch.AddKey(id);

        await batch.ExecuteAsync(ct);
        return batch.Results;
    }
}
