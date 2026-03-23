using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IDynamoDBContext _context, IAmazonDynamoDB _client) : IDishRepository
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

    public async Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(string locationId, CancellationToken 
            cancellationToken = default)
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
    
    public async Task<Dish?> GetDishByIdAsync(string dishId, CancellationToken ct = default)
    {
        return await _context.LoadAsync<Dish>(dishId, ct);
    }

    // !!!IMPORTANT(шоб не забути)!!! So basically here we don't need GSI since the amount of dishes is not big and 
    //probably initially we will load all the dishes. But if we add GSI we can move to QueryRequest which will reduce 
    // RCU cost.
    public async Task<IReadOnlyList<DishBriefDTO>> GetShortenedDishesAsync(string? type, string sort,
        CancellationToken ct = default)
    {
        var request = new ScanRequest
        {
            TableName = "Dishes",
            ProjectionExpression = "id, #n, dishType, price, imageUrl, #w, state",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#n", "name" },
                { "#w", "weight" }
            }
        };

        if (!string.IsNullOrEmpty(type))
        {
            request.FilterExpression = "dishType = :type";
            request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":type", new AttributeValue { S = type } }
            };
        }
        
        var results = new List<DishBriefDTO>();
        Dictionary<string, AttributeValue>? lastKey = null;
        
        do
        {
            if (lastKey is not null)
                request.ExclusiveStartKey = lastKey;

            var response = await _client.ScanAsync(request, ct);

            results.AddRange(response.Items.Select(MapToDishBrief));

            lastKey = response.LastEvaluatedKey?.Count > 0
                ? response.LastEvaluatedKey
                : null;

        } while (lastKey is not null);

        return ApplySort(results, sort);
    }
    
    private static DishBriefDTO MapToDishBrief(Dictionary<string, AttributeValue> item) => new()
    {
        Id       = item.TryGetValue("id", out var id)           ? id.S             : string.Empty,
        Name     = item.TryGetValue("name", out var name)       ? name.S           : string.Empty,
        DishType = item.TryGetValue("dishType", out var type)   ? type.S           : string.Empty,
        Price    = item.TryGetValue("price", out var price)     ? float.Parse(price.N) : 0f,
        ImageUrl = item.TryGetValue("imageUrl", out var imgUrl) ? imgUrl.S         : null,
        Weight   = item.TryGetValue("weight", out var weight)   ? int.Parse(weight.N) : null,
        State    = item.TryGetValue("state", out var state)     ? state.S          : "ON"
    };
    
    private static IReadOnlyList<DishBriefDTO> ApplySort(List<DishBriefDTO> dishes, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.TrimEntries);
        var property  = parts.ElementAtOrDefault(0)?.ToLower() ?? "name";
        var direction = parts.ElementAtOrDefault(1)?.ToLower() ?? "asc";

        Func<DishBriefDTO, object> keySelector = property switch
        {
            "name"     => d => d.Name,
            "dishtype" => d => d.DishType,
            "price"    => d => d.Price,
            "weight"   => d => (object)(d.Weight ?? 0),
            _          => d => d.Name
        };

        return direction == "desc"
            ? dishes.OrderByDescending(keySelector).ToList()
            : dishes.OrderBy(keySelector).ToList();
    }
}
