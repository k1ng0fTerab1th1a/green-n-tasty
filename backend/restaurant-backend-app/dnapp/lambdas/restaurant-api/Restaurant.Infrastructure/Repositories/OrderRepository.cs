using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using System.Globalization;

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

    public async Task<Order?> GetByReservationIdAsync(string reservationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reservationId))
            return null;

        return await _context.LoadAsync<Order>(reservationId, ct);
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
                        UpdateExpression = "ADD dishCount :dishCountDelta SET updatedAt = :updatedAt",
                        ConditionExpression =
                            "attribute_exists(id) AND waiterId = :waiterId AND dishCount = :zero AND #status = :expectedStatus",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#status"] = "status"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":dishCountDelta"] = new() { N = dishCount.ToString(CultureInfo.InvariantCulture) },
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

    public async Task<bool> UpdateWithReservationDishCountAsync(
        Order order,
        int expectedOrderVersion,
        string operationId,
        int dishCountDelta,
        string updatedAt,
        CancellationToken ct = default)
    {
        var orderItem = _context.ToDocument(order).ToAttributeMap();

        var orderUpdate = new Update
        {
            TableName = "Orders",
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = order.Id }
            },
            UpdateExpression =
                "SET dishes = :dishes, totalAmount = :totalAmount " +
                "ADD #version :versionIncrement, #processedOperationIds :operationIds",
            ConditionExpression =
                "attribute_exists(id) " +
                "AND waiterId = :waiterId " +
                "AND #status = :expectedOrderStatus " +
                "AND (attribute_not_exists(#version) OR #version = :expectedVersion) " +
                "AND (attribute_not_exists(#processedOperationIds) OR NOT contains(#processedOperationIds, :operationId))",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status",
                ["#version"] = "version",
                ["#processedOperationIds"] = "processedOperationIds"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":dishes"] = orderItem["dishes"],
                [":totalAmount"] = orderItem["totalAmount"],
                [":waiterId"] = new() { S = order.WaiterId },
                [":expectedOrderStatus"] = new() { S = OrderStatus.Open.ToString() },
                [":expectedVersion"] = new() { N = expectedOrderVersion.ToString(CultureInfo.InvariantCulture) },
                [":versionIncrement"] = new() { N = "1" },
                [":operationId"] = new() { S = operationId },
                [":operationIds"] = new() { SS = new List<string> { operationId } }
            }
        };

        var reservationValues = new Dictionary<string, AttributeValue>
        {
            [":dishCountDelta"] = new() { N = dishCountDelta.ToString(CultureInfo.InvariantCulture) },
            [":updatedAt"] = new() { S = updatedAt },
            [":waiterId"] = new() { S = order.WaiterId },
            [":expectedReservationStatus"] = new() { S = ReservationStatus.InProgress.ToString() }
        };

        var reservationCondition =
            "attribute_exists(id) AND waiterId = :waiterId AND #status = :expectedReservationStatus";

        if (dishCountDelta < 0)
        {
            reservationValues[":requiredDishCount"] =
                new() { N = Math.Abs(dishCountDelta).ToString(CultureInfo.InvariantCulture) };

            reservationCondition += " AND dishCount >= :requiredDishCount";
        }

        var reservationUpdate = new Update
        {
            TableName = "Reservations",
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = order.ReservationId }
            },
            UpdateExpression = "ADD dishCount :dishCountDelta SET updatedAt = :updatedAt",
            ConditionExpression = reservationCondition,
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status"
            },
            ExpressionAttributeValues = reservationValues
        };

        var request = new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
        {
            new() { Update = orderUpdate },
            new() { Update = reservationUpdate }
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

    public async Task<bool> CompleteAsync(
        Order order,
        string waiterId,
        int expectedOrderVersion,
        string operationId,
        CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = "Orders",
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = order.Id }
            },
            UpdateExpression =
                "SET #status = :newStatus, completedAt = :completedAt " +
                "ADD #version :versionIncrement, #processedOperationIds :operationIds",
            ConditionExpression =
                "attribute_exists(id) " +
                "AND waiterId = :waiterId " +
                "AND #status = :expectedStatus " +
                "AND (attribute_not_exists(#version) OR #version = :expectedVersion) " +
                "AND (attribute_not_exists(#processedOperationIds) OR NOT contains(#processedOperationIds, :operationId))",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status",
                ["#version"] = "version",
                ["#processedOperationIds"] = "processedOperationIds"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newStatus"] = new() { S = OrderStatus.Completed.ToString() },
                [":completedAt"] = new() { S = order.CompletedAt! },
                [":waiterId"] = new() { S = waiterId },
                [":expectedStatus"] = new() { S = OrderStatus.Open.ToString() },
                [":expectedVersion"] = new() { N = expectedOrderVersion.ToString(CultureInfo.InvariantCulture) },
                [":versionIncrement"] = new() { N = "1" },
                [":operationId"] = new() { S = operationId },
                [":operationIds"] = new() { SS = new List<string> { operationId } }
            },
            ReturnValues = ReturnValue.NONE
        };

        try
        {
            await _dynamoDb.UpdateItemAsync(request, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<bool> CompleteOnReservationFinishIfOpenAsync(
        string reservationId,
        string waiterId,
        string completedAt,
        CancellationToken ct = default)
    {
        var order = await GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return true;

        if (order.Status != OrderStatus.Open)
            return true;

        var request = new UpdateItemRequest
        {
            TableName = "Orders",
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = order.Id }
            },
            UpdateExpression =
                "SET #status = :newStatus, completedAt = :completedAt " +
                "ADD #version :versionIncrement",
            ConditionExpression =
                "attribute_exists(id) " +
                "AND waiterId = :waiterId " +
                "AND #status = :expectedStatus " +
                "AND (attribute_not_exists(#version) OR #version = :expectedVersion)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status",
                ["#version"] = "version"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newStatus"] = new() { S = OrderStatus.Completed.ToString() },
                [":completedAt"] = new() { S = completedAt },
                [":waiterId"] = new() { S = waiterId },
                [":expectedStatus"] = new() { S = OrderStatus.Open.ToString() },
                [":expectedVersion"] = new() { N = order.Version.ToString(CultureInfo.InvariantCulture) },
                [":versionIncrement"] = new() { N = "1" }
            }
        };

        try
        {
            await _dynamoDb.UpdateItemAsync(request, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
}
