using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using System.Text;

namespace Restaurant.Infrastructure.Repositories;

public class FeedbackRepository(IDynamoDBContext context,
    IAmazonDynamoDB client) : IFeedbackRepository
{
    public async Task SaveAsync(Feedback feedback, CancellationToken ct = default)
    {
        feedback.LocationIdAndType = $"{feedback.LocationId}#{feedback.Type}";
        await context.SaveAsync(feedback, ct);
    }

    public async Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type = "waiter", List<string>? sort = null, string? pageToken = null)
    {
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;
        if (!string.IsNullOrEmpty(pageToken))
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(pageToken));
            var doc = Document.FromJson(json);
            exclusiveStartKey = doc.ToAttributeMap();
        }

        string indexName = "LocationType-Date-Index";
        bool isAscending = true;

        if (sort != null && sort.Count > 0)
        {
            var sortStr = sort[0].ToLower();
            if (sortStr.Contains("rate")) indexName = "LocationType-Rate-Index";
            if (sortStr.Contains("desc")) isAscending = false;
        }

        var request = new QueryRequest
        {
            TableName = "Feedbacks",
            IndexName = indexName,
            KeyConditionExpression = "#lt = :locType",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#lt", "locationId#type" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":locType", new AttributeValue($"{locationId}#{type}") }
            },
            Limit = size,
            ExclusiveStartKey = exclusiveStartKey,
            ScanIndexForward = isAscending
        };

        var response = await client.QueryAsync(request);

        string? nextToken = null;
        if (response.LastEvaluatedKey?.Count > 0)
        {
            var doc = Document.FromAttributeMap(response.LastEvaluatedKey);
            var json = doc.ToJson();
            nextToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        return new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = response.Items.Select(item => context.FromDocument<Feedback>(Document.FromAttributeMap(item))).ToList(),
            NextPageToken = nextToken
        };
    }
    
    public async Task SaveBatchAsync(IEnumerable<Feedback> feedbacks, CancellationToken ct = default)
    {
        var batch = context.CreateBatchWrite<Feedback>();

        foreach (var feedback in feedbacks)
        {
            feedback.LocationIdAndType = $"{feedback.LocationId}#{feedback.Type}";
            batch.AddPutItem(feedback);
        }

        await batch.ExecuteAsync(ct);
    }
}