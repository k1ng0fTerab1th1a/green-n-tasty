using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public OrderRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public async Task CreateAsync(Order order, CancellationToken ct = default)
    {
        await _context.SaveAsync(order, ct);
    }

    public async Task<bool> CreateWithReservationUpdateAsync(
        Order order,
        string reservationId,
        string waiterId,
        int dishCount,
        string updatedAt,
        CancellationToken ct = default)
    {
        var orderItem = _context.ToDocument(order).ToAttributeMap();

        var request = new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = "Orders",
                        Item = orderItem,
                        ConditionExpression = "attribute_not_exists(id)"
                    }
                },
                new()
                {
                    Update = new Update
                    {
                        TableName = "Reservations",
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["id"] = new() { S = reservationId }
                        },
                        UpdateExpression = "SET dishCount = :dishCount, updatedAt = :updatedAt",
                        ConditionExpression = "attribute_exists(id) AND waiterId = :waiterId AND dishCount = :zero AND #status = :expectedStatus",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#status"] = "status"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":dishCount"] = new() { N = dishCount.ToString() },
                            [":updatedAt"] = new() { S = updatedAt },
                            [":waiterId"] = new() { S = waiterId },
                            [":zero"] = new() { N = "0" },
                            [":expectedStatus"] = new() { S = ReservationStatus.InProgress.ToString() }
                        }
                    }
                }
            }
        };

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(request, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }
}
