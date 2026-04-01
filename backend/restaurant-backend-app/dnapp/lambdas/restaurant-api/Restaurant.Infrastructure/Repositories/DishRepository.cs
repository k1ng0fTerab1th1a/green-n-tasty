using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using System.Globalization;
using System.Text;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IDynamoDBContext _context, IAmazonDynamoDB _client) : IDishRepository
{
    private const string DishesTable = "Dishes";
    private const string DishSearchTable = "DishSearch";

    public async Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = "PopularDishesIndex"
        };

        var asyncSearch = _context.QueryAsync<Dish>("true", config);
        return await asyncSearch.GetRemainingAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(
        string locationId,
        CancellationToken cancellationToken = default)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = "SpecialityIndex"
        };

        var asyncSearch = _context.QueryAsync<Dish>(locationId, config);
        return await asyncSearch.GetRemainingAsync(cancellationToken);
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

    public async Task IncrementPopularityAsync(string dishId, int quantity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dishId) || quantity <= 0)
            return;

        var request = new UpdateItemRequest
        {
            TableName = DishesTable,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = dishId }
            },
            UpdateExpression = "ADD popularity :increment SET popularityFlag = :popularityFlag",
            ConditionExpression = "attribute_exists(id)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":increment"] = new() { N = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                [":popularityFlag"] = new() { S = "true" }
            }
        };

        await _client.UpdateItemAsync(request, ct);
    }

    // !!!IMPORTANT(шоб не забути)!!! So basically here we don't need GSI since the amount of dishes is not big and 
    //probably initially we will load all the dishes. But if we add GSI we can move to QueryRequest which will reduce 
    // RCU cost.
    public async Task<IReadOnlyList<DishBriefDTO>> GetShortenedDishesAsync(
     string? type,
     string sort,
     CancellationToken ct = default)
    {
        var request = new ScanRequest
        {
            TableName = DishesTable,
            ProjectionExpression = "id, #n, dishType, price, imageUrl, #w, #s, popularity",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#n"] = "name",
                ["#w"] = "weight",
                ["#s"] = "state"
            }
        };

        if (!string.IsNullOrEmpty(type))
        {
            request.FilterExpression = "dishType = :type";
            request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":type"] = new() { S = type }
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

    public async Task<IReadOnlyList<DishSearchResultDTO>> SearchAsync(
        string query,
        string? type,
        int limit,
        CancellationToken ct = default)
    {
        var tokens = TokenizeQuery(query);
        if (tokens.Count == 0)
            return Array.Empty<DishSearchResultDTO>();

        Dictionary<string, DishSearchItem>? intersection = null;

        foreach (var token in tokens)
        {
            var tokenMatches = await QueryByTokenAsync(token, ct);

            var filtered = tokenMatches
                .GroupBy(x => x.DishId)
                .ToDictionary(g => g.Key, g => g.First());

            if (intersection is null)
            {
                intersection = filtered;
            }
            else
            {
                var commonKeys = intersection.Keys.Intersect(filtered.Keys).ToList();
                intersection = commonKeys.ToDictionary(key => key, key => filtered[key]);
            }

            if (intersection.Count == 0)
                return Array.Empty<DishSearchResultDTO>();
        }

        var dishes = await GetByIdsAsync(intersection.Keys, ct);
        var normalizedQuery = NormalizeText(query);
        var firstToken = tokens[0];

        return dishes
            .Where(d => IsDishEnabled(d.State))
            .Where(d => string.IsNullOrWhiteSpace(type) ||
                        string.Equals(d.DishType, type, StringComparison.OrdinalIgnoreCase))
            .Select(d => new
            {
                Dish = d,
                NameNormalized = NormalizeText(d.Name)
            })
            .OrderByDescending(x => string.Equals(x.NameNormalized, normalizedQuery, StringComparison.Ordinal))
            .ThenByDescending(x => x.NameNormalized.StartsWith(normalizedQuery, StringComparison.Ordinal))
            .ThenBy(x => x.Dish.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => new DishSearchResultDTO
            {
                Id = x.Dish.Id,
                Name = x.Dish.Name,
                DishType = x.Dish.DishType,
                Token = firstToken
            })
            .ToList();
    }
    private static bool IsDishEnabled(string? state)
        => string.Equals(state, "ON", StringComparison.OrdinalIgnoreCase);
    public async Task<int> RebuildSearchIndexAsync(CancellationToken ct = default)
    {
        var dishes = await ScanAllDishesAsync(ct);

        var searchItems = dishes
            .SelectMany(BuildSearchItems)
            .GroupBy(x => new { x.Token, x.DishId })
            .Select(g => g.First())
            .ToList();

        await DeleteAllSearchItemsAsync(ct);
        await BatchPutSearchItemsAsync(searchItems, ct);

        return searchItems.Count;
    }

    public async Task UpsertDishSearchIndexAsync(Dish dish, CancellationToken ct = default)
    {
        await RemoveDishSearchIndexAsync(dish.Id, ct);

        var searchItems = BuildSearchItems(dish)
            .GroupBy(x => new { x.Token, x.DishId })
            .Select(g => g.First())
            .ToList();

        await BatchPutSearchItemsAsync(searchItems, ct);
    }

    public async Task RemoveDishSearchIndexAsync(string dishId, CancellationToken ct = default)
    {
        var keys = new List<DishSearchItem>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var request = new ScanRequest
            {
                TableName = DishSearchTable,
                ProjectionExpression = "#token, #dishId",
                FilterExpression = "#dishId = :dishId",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#token"] = "token",
                    ["#dishId"] = "dishId"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":dishId"] = new() { S = dishId }
                },
                ExclusiveStartKey = lastKey
            };

            var response = await _client.ScanAsync(request, ct);

            keys.AddRange(response.Items.Select(item => new DishSearchItem
            {
                Token = item["token"].S,
                DishId = item["dishId"].S
            }));

            lastKey = response.LastEvaluatedKey?.Count > 0
                ? response.LastEvaluatedKey
                : null;

        } while (lastKey is not null);

        foreach (var chunk in keys.Chunk(25))
        {
            var writes = chunk
                .Select(item => new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["token"] = new() { S = item.Token },
                            ["dishId"] = new() { S = item.DishId }
                        }
                    }
                })
                .ToList();

            await ExecuteBatchWriteAsync(writes, ct);
        }
    }

    private async Task<List<Dish>> ScanAllDishesAsync(CancellationToken ct)
    {
        var search = _context.ScanAsync<Dish>(new List<ScanCondition>());
        return await search.GetRemainingAsync(ct);
    }

    private async Task<List<DishSearchItem>> QueryByTokenAsync(string token, CancellationToken ct)
    {
        var results = new List<DishSearchItem>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var request = new QueryRequest
            {
                TableName = DishSearchTable,
                KeyConditionExpression = "#token = :token",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#token"] = "token"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":token"] = new() { S = token }
                },
                ExclusiveStartKey = lastKey
            };

            var response = await _client.QueryAsync(request, ct);
            results.AddRange(response.Items.Select(MapSearchItem));

            lastKey = response.LastEvaluatedKey?.Count > 0
                ? response.LastEvaluatedKey
                : null;

        } while (lastKey is not null);

        return results;
    }

    private async Task DeleteAllSearchItemsAsync(CancellationToken ct)
    {
        var keys = new List<DishSearchItem>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var request = new ScanRequest
            {
                TableName = DishSearchTable,
                ProjectionExpression = "#token, #dishId",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#token"] = "token",
                    ["#dishId"] = "dishId"
                },
                ExclusiveStartKey = lastKey
            };

            var response = await _client.ScanAsync(request, ct);

            keys.AddRange(response.Items.Select(item => new DishSearchItem
            {
                Token = item["token"].S,
                DishId = item["dishId"].S
            }));

            lastKey = response.LastEvaluatedKey?.Count > 0
                ? response.LastEvaluatedKey
                : null;

        } while (lastKey is not null);

        foreach (var chunk in keys.Chunk(25))
        {
            var writes = chunk
                .Select(item => new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["token"] = new() { S = item.Token },
                            ["dishId"] = new() { S = item.DishId }
                        }
                    }
                })
                .ToList();

            await ExecuteBatchWriteAsync(writes, ct);
        }
    }

    private async Task BatchPutSearchItemsAsync(List<DishSearchItem> items, CancellationToken ct)
    {
        if (items.Count == 0)
            return;

        foreach (var chunk in items.Chunk(25))
        {
            var writes = chunk
                .Select(item => new WriteRequest
                {
                    PutRequest = new PutRequest
                    {
                        Item = _context.ToDocument(item).ToAttributeMap()
                    }
                })
                .ToList();

            await ExecuteBatchWriteAsync(writes, ct);
        }
    }

    private async Task ExecuteBatchWriteAsync(List<WriteRequest> writes, CancellationToken ct)
    {
        var pending = new Dictionary<string, List<WriteRequest>>
        {
            [DishSearchTable] = writes
        };

        while (pending.Count > 0)
        {
            var response = await _client.BatchWriteItemAsync(
                new BatchWriteItemRequest
                {
                    RequestItems = pending
                },
                ct);

            pending = response.UnprocessedItems
                .Where(x => x.Value.Count > 0)
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    private static IEnumerable<DishSearchItem> BuildSearchItems(Dish dish)
    {
        if (string.IsNullOrWhiteSpace(dish.Name))
            yield break;

        var nameNormalized = NormalizeText(dish.Name);
        var words = TokenizeQuery(dish.Name);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var word in words)
        {
            for (var i = 2; i <= word.Length; i++)
            {
                var prefix = word[..i];
                if (!seen.Add(prefix))
                    continue;

                yield return new DishSearchItem
                {
                    Token = prefix,
                    DishId = dish.Id,
                    NameNormalized = nameNormalized,
                    Name = dish.Name
                };
            }
        }
    }

    private static DishBriefDTO MapToDishBrief(Dictionary<string, AttributeValue> item) => new()
    {
        Id = item.TryGetValue("id", out var id) ? id.S : string.Empty,
        Name = item.TryGetValue("name", out var name) ? name.S : string.Empty,
        DishType = item.TryGetValue("dishType", out var type) ? type.S : string.Empty,
        Price = item.TryGetValue("price", out var price)
            ? decimal.Parse(price.N, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture)
            : 0m,
        ImageUrl = item.TryGetValue("imageUrl", out var imgUrl) ? imgUrl.S : null,
        Weight = item.TryGetValue("weight", out var weight)
            ? int.Parse(weight.N, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture)
            : null,
        Popularity = item.TryGetValue("popularity", out var popularity)
            ? int.Parse(popularity.N, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture)
            : 0,
        State = item.TryGetValue("state", out var state) ? state.S : "ON"
    };

    private static DishSearchItem MapSearchItem(Dictionary<string, AttributeValue> item) => new()
    {
        Token = item.TryGetValue("token", out var token) ? token.S : string.Empty,
        DishId = item.TryGetValue("dishId", out var dishId) ? dishId.S : string.Empty,
        NameNormalized = item.TryGetValue("nameNormalized", out var normalized) ? normalized.S : string.Empty,
        Name = item.TryGetValue("name", out var name) ? name.S : string.Empty
    };

    private static DishBriefDTO MapSearchItemToDishBrief(DishSearchItem item) => new()
    {
        Id = item.DishId,
        Name = item.Name
    };

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(' ', ExtractWords(value));
    }

    private static List<string> TokenizeQuery(string value)
    {
        return ExtractWords(value)
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> ExtractWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var source = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(source.Length);

        foreach (var ch in source)
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');

        return builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static IReadOnlyList<DishBriefDTO> ApplySort(List<DishBriefDTO> dishes, string sort)
    {
        var parts = sort.Split(',', StringSplitOptions.TrimEntries);
        var property = parts.ElementAtOrDefault(0)?.ToLower() ?? "name";
        var direction = parts.ElementAtOrDefault(1)?.ToLower() ?? "asc";

        Func<DishBriefDTO, object> keySelector = property switch
        {
            "name" => d => d.Name,
            "dishtype" => d => d.DishType,
            "price" => d => d.Price,
            "weight" => d => d.Weight ?? 0,
            "popularity" => d => d.Popularity,
            _ => d => d.Name
        };

        return direction == "desc"
            ? dishes.OrderByDescending(keySelector).ToList()
            : dishes.OrderBy(keySelector).ToList();
    }
}
