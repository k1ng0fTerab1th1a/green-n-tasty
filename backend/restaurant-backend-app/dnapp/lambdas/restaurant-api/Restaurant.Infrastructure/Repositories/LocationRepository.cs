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
    
    public async Task UpdateUserRatingAsync(string locationId, double newFeedbackRating, CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Locations",
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = locationId } }
            },
            UpdateExpression = "ADD #s :newRating, #c :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#r", "rating" },
                { "#c", "feedbacksNumber" },
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":newRating", new AttributeValue { N = newFeedbackRating.ToString("F2") } },
                { ":inc", new AttributeValue { N = "1" } },
                { ":zero", new AttributeValue { N = "0" } }
            },
            ReturnValues = ReturnValue.NONE // can be set to ALL_NEW if we really need to send it to the client
        };

        await _client.UpdateItemAsync(request, ct);
    }
}
