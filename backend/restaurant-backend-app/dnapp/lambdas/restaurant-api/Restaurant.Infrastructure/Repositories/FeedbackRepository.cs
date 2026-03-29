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

    public async Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type = "waiter", List<string>? sort = null, string? pageToken = null, CancellationToken ct = default)
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

        var response = await client.QueryAsync(request, ct);

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

    public async Task<string?> GetSecretCodeByReservationIdAsync(string reservationId, CancellationToken ct = default)
    {
        var request = new GetItemRequest
        {
            TableName = "Reservations",
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = reservationId } }
            },
            ProjectionExpression = "secretCode"
        };

        var response = await client.GetItemAsync(request, ct);
        return response.Item.TryGetValue("secretCode", out var attr)
            ? attr.S
            : null;
    }

    public async Task<bool> IsFeedbackAlreadyMade(string reservationId, string feedbackType, CancellationToken ct)
    {
        var request = new QueryRequest
        {
            TableName                 = "Feedbacks",
            IndexName                 = "reservationId-index",
            KeyConditionExpression    = "reservationId = :rid",
            FilterExpression          = "#t = :type",
            ExpressionAttributeNames  = new Dictionary<string, string>
            {
                ["#t"] = "type"  // "type" is a reserved word in DynamoDB
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":rid"]  = new AttributeValue { S = reservationId },
                [":type"] = new AttributeValue { S = feedbackType }
            },
            Select = Select.COUNT  // we only need the count, skip deserializing documents
        };

        var response = await client.QueryAsync(request, ct);
        return response.Count > 0;
    }
}