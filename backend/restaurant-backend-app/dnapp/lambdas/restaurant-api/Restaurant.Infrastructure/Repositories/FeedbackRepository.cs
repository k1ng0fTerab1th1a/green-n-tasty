using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Interfaces;
using Restaurant.Core.ServiceDTOs;
using Restaurant.Core.Services;

namespace Restaurant.Infrastructure.Repositories;

public class FeedbackRepository(IDynamoDBContext context, 
    IAmazonDynamoDB client, IRawMappingService mappingService) : IFeedbackRepository
{
    public async Task SaveAsync(Feedback feedback)
    {
        feedback.LocationIdAndType = $"{feedback.LocationId}#{feedback.Type}";
        await context.SaveAsync(feedback);
    }

    public async Task<FeedbackPaginatedDBResponseDto> GetByLocationAsync(string locationId,
        int size, string type = "waiter", string? pageToken = null)
    {
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;
        if (!string.IsNullOrEmpty(pageToken))
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(pageToken));
            exclusiveStartKey = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(json);
        }

        var request = new QueryRequest
        {
            TableName = "Feedbacks",
            IndexName = "locationIdType-index",
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
            ExclusiveStartKey = exclusiveStartKey
        };

        var response = await client.QueryAsync(request);

        string? nextToken = null;
        if (response.LastEvaluatedKey?.Count > 0)
        {
            var json = JsonSerializer.Serialize(response.LastEvaluatedKey);
            nextToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        var pageCountData = await GetTotalPagedValues(locationId, size, type);

        return new FeedbackPaginatedDBResponseDto
        {
            TotalPages = pageCountData.totalPages,
            TotalSize = pageCountData.totalSize,
            Feedbacks = mappingService.MapToFeedback(response.Items).ToList(),
            NextPageToken = nextToken
        };
    }

    private async Task<(int totalSize, int totalPages)> GetTotalPagedValues(string locationId, int size, string type)
    {
        var countRequest = new QueryRequest
        {
            TableName = "Feedbacks",
            IndexName = "locationIdType-index",
            KeyConditionExpression = "#lt = :locType",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#lt", "locationId#type" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":locType", new AttributeValue($"{locationId}#{type}") }
            },
            Select = Select.COUNT
        };

        var countResponse = await client.QueryAsync(countRequest);
        var totalPages = (int)Math.Ceiling(countResponse.Count / (double)size);

        return (countResponse.Count, totalPages);
    }
}