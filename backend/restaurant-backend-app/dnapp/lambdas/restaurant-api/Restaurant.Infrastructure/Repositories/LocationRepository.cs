using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{

    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;

    public LocationRepository(IDynamoDBContext context, IAmazonDynamoDB client)
    {
        _context = context;
        _client = client;
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var search = _context.ScanAsync<Location>(new List<ScanCondition>());
        return await search.GetRemainingAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
    {
        return GetLocationsAsync(cancellationToken);
    }

    public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _context.LoadAsync<Location?>(id, cancellationToken);
    }
    
    public async Task UpdateKitchenRatingAsync(string locationId, int newFeedbackRating, CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Locations",
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = locationId } }
            },
            UpdateExpression = "ADD #r :newRating, #c :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#r", "rating" },
                { "#c", "feedbacksAmount" },
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":newRating", new AttributeValue { N = newFeedbackRating.ToString() } },
                { ":inc",       new AttributeValue { N = "1" } },
            },
            ReturnValues = ReturnValue.NONE
        };

        await _client.UpdateItemAsync(request, ct);
    }

    public async Task<(int rating, int feedbacksAmount)> GetLocationFeedbacksDataAsync(string locationId, 
        CancellationToken ct = default)
    {
        var request = new GetItemRequest
        {
            TableName = "Locations",
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = locationId } }
            },
            ProjectionExpression = "#r, #f",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#r", "rating" },
                { "#f", "feedbacksAmount" }
            }
        };

        var response = await _client.GetItemAsync(request, ct);

        if (!response.IsItemSet)
            throw new KeyNotFoundException($"Location {locationId} not found");

        var rating = response.Item.TryGetValue("rating", out var r) && !string.IsNullOrEmpty(r.N)
            ? int.Parse(r.N)
            : 0;

        var feedbacksAmount = response.Item.TryGetValue("feedbacksAmount", out var f) && !string.IsNullOrEmpty(f.N)
            ? int.Parse(f.N)
            : 0;

        return (rating, feedbacksAmount);
    }

    public async Task UpdateAsync(Location location, CancellationToken ct)
    {
        await _context.SaveAsync(location, ct);
    }
}
