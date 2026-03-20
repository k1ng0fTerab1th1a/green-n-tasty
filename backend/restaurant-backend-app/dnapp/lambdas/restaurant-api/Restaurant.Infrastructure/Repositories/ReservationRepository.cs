using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;


namespace Restaurant.Infrastructure.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private const string CustomerIndex = "customerId-start-index";
    private const string WaiterIndex = "waiterId-start-index";
    private const string TableIndex = "tableKey-start-index";

    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;

    public ReservationRepository(IDynamoDBContext context, IAmazonDynamoDB dynamoDb)
    {
        _context = context;
        _dynamoDb = dynamoDb;
    }

    public async Task<Reservation?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var item = await _context.LoadAsync<Reservation>(id, ct);
        return item;
    }

    public async Task<IReadOnlyList<Reservation>> QueryByCustomerAsync(
        string customerId,
        string? startFromIso = null,
        string? startToIso = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Array.Empty<Reservation>();

        var op = new DynamoDBOperationConfig { IndexName = CustomerIndex };

        AsyncSearch<Reservation> search;
        if (!string.IsNullOrWhiteSpace(startFromIso) && !string.IsNullOrWhiteSpace(startToIso))
        {
            search = _context.QueryAsync<Reservation>(
                customerId,
                QueryOperator.Between,
                new[] { startFromIso!, startToIso! },
                op);
        }
        else
        {
            search = _context.QueryAsync<Reservation>(customerId, op);
        }

        return await search.GetRemainingAsync(ct);
    }

    public async Task<IReadOnlyList<Reservation>> QueryByWaiterAsync(
        string waiterId,
        string? startFromIso = null,
        string? startToIso = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(waiterId))
            return Array.Empty<Reservation>();

        var op = new DynamoDBOperationConfig { IndexName = WaiterIndex };

        AsyncSearch<Reservation> search;
        if (!string.IsNullOrWhiteSpace(startFromIso) && !string.IsNullOrWhiteSpace(startToIso))
        {
            search = _context.QueryAsync<Reservation>(
                waiterId,
                QueryOperator.Between,
                new[] { startFromIso!, startToIso! },
                op);
        }
        else
        {
            search = _context.QueryAsync<Reservation>(waiterId, op);
        }

        return await search.GetRemainingAsync(ct);
    }

    public async Task<bool> CreateWithSlotsAsync(Reservation reservation, DateOnly date, List<string> slots, CancellationToken ct = default)
    {
        var reservationItem = _context.ToDocument(reservation).ToAttributeMap();

        var conditionParts = slots.Select((_, i) => $"NOT contains(reservedSlots, :s{i})");
        var conditionExpression = string.Join(" AND ", conditionParts);

        var expressionValues = slots
            .Select((slot, i) => (Key: $":s{i}", Value: slot))
            .ToDictionary(
                x => x.Key,
                x => new AttributeValue { S = x.Value });

        expressionValues[":newSlots"] = new AttributeValue { SS = slots };

        var ttl = new DateTimeOffset(date.AddDays(2).ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();
        expressionValues[":ttl"] = new AttributeValue { N = ttl.ToString() };

        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Put = new Put
                {
                    TableName           = "Reservations",
                    Item                = reservationItem,
                    ConditionExpression = "attribute_not_exists(id)"
                }
            },
            new()
            {
                Update = new Update
                {
                    TableName        = "TableDays",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["tableKey"] = new() { S = reservation.TableKey },
                        ["date"]     = new() { S = date.ToString("yyyy-MM-dd") }
                    },
                    UpdateExpression = "ADD reservedSlots :newSlots SET #ttl = if_not_exists(#ttl, :ttl)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#ttl"] = "ttl"
                    },
                    ConditionExpression          = conditionExpression,
                    ExpressionAttributeValues    = expressionValues
                }
            }
        };

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = transactItems }, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<Reservation>> QueryByTableAsync(
        string tableKey,
        string startFromIso,
        string startToIso,
        CancellationToken ct)
    {
        var op = new DynamoDBOperationConfig { IndexName = TableIndex };

        var search = _context.QueryAsync<Reservation>(
            tableKey,
            QueryOperator.Between,
            new[] { startFromIso, startToIso },
            op);

        return await search.GetRemainingAsync(ct);
    }

    public async Task<bool> CancelReservationAsync(Reservation reservation, List<string> slots, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(DateTimeOffset.Parse(reservation.StartDateTime).DateTime);

        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Update = new Update
                {
                    TableName = "Reservations",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new() { S = reservation.Id }
                    },
                    UpdateExpression = "SET #s = :cancelled, updatedAt = :updatedAt",
                    ConditionExpression = "attribute_exists(id)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#s"] = "status"
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":cancelled"] = new() { S = ReservationStatus.Cancelled.ToString() },
                        [":updatedAt"]  = new() { S = DateTime.UtcNow.ToString("O") }
                    }
                }
            },

            // Remove slots from TableDays
            new()
            {
                Update = new Update
                {
                    TableName = "TableDays",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["tableKey"] = new() { S = reservation.TableKey },
                        ["date"]     = new() { S = date.ToString("yyyy-MM-dd") }
                    },
                    UpdateExpression = "DELETE reservedSlots :slots",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":slots"] = new() { SS = slots }
                    }
                }
            }
        };

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = transactItems }, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public async Task<Reservation?> UpdateReservationAsync(
        Reservation reservation,
        List<string> newSlots,
        List<string> oldSlots,
        string oldTableKey,
        DateTimeOffset oldStart,
        CancellationToken ct = default)
    {
        var oldDate = DateOnly.FromDateTime(oldStart.DateTime);
        var newDate = DateOnly.FromDateTime(DateTimeOffset.Parse(reservation.StartDateTime).DateTime);
        var ttl = ComputeTtl(newDate);

        var reservationItem = _context.ToDocument(reservation).ToAttributeMap();

        bool isTableDifferent = oldTableKey != reservation.TableKey;
        bool isDayDifferent = oldDate != newDate;

        if (!isTableDifferent && !isDayDifferent)
            await UpdateSameTableSameDayAsync(reservationItem, newSlots, oldSlots, reservation.TableKey, newDate, ttl, ct);
        else
            await UpdateDifferentTableOrDayAsync(reservationItem, newSlots, oldSlots, reservation.TableKey, oldTableKey, newDate, oldDate, ttl, ct);

        return reservation;
    }

    public async Task<bool> UpdateLifecycleAsync(
        Reservation reservation,
        ReservationStatus expectedCurrentStatus,
        ReservationStatus newStatus,
        List<string>? slotsToRelease = null,
        CancellationToken ct = default)
    {
        var values = new Dictionary<string, AttributeValue>
        {
            [":expectedStatus"] = new() { S = expectedCurrentStatus.ToString() },
            [":newStatus"] = new() { S = newStatus.ToString() },
            [":updatedAt"] = new() { S = reservation.UpdatedAt }
        };

        var names = new Dictionary<string, string>
        {
            ["#status"] = "status"
        };

        var setters = new List<string>
        {
            "#status = :newStatus",
            "updatedAt = :updatedAt"
        };

        if (reservation.ActualStartTime is not null)
        {
            values[":actualStartTime"] = new() { S = reservation.ActualStartTime };
            setters.Add("actualStartTime = :actualStartTime");
        }

        if (reservation.ActualEndTime is not null)
        {
            values[":actualEndTime"] = new() { S = reservation.ActualEndTime };
            setters.Add("actualEndTime = :actualEndTime");
        }

        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Update = new Update
                {
                    TableName = "Reservations",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new() { S = reservation.Id }
                    },
                    UpdateExpression = $"SET {string.Join(", ", setters)}",
                    ConditionExpression = "attribute_exists(id) AND #status = :expectedStatus",
                    ExpressionAttributeNames = names,
                    ExpressionAttributeValues = values
                }
            }
        };

        if (slotsToRelease is { Count: > 0 })
        {
            var date = DateOnly.FromDateTime(DateTimeOffset.Parse(reservation.StartDateTime).DateTime);
            transactItems.Add(new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = "TableDays",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["tableKey"] = new() { S = reservation.TableKey },
                        ["date"] = new() { S = date.ToString("yyyy-MM-dd") }
                    },
                    UpdateExpression = "DELETE reservedSlots :slots",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":slots"] = new() { SS = slotsToRelease }
                    }
                }
            });
        }

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            }, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    private async Task UpdateSameTableSameDayAsync(
        Dictionary<string, AttributeValue> reservationItem,
        List<string> newSlots,
        List<string> oldSlots,
        string tableKey,
        DateOnly date,
        long ttl,
        CancellationToken ct)
    {
        var slotsToAdd = newSlots.Except(oldSlots).ToList();
        var slotsToRemove = oldSlots.Except(newSlots).ToList();

        if (slotsToAdd.Any())
            await AddSlotsWithOverlapCheckAsync(reservationItem, slotsToAdd, tableKey, date, ttl, ct);
        else
            await UpdateReservationItemOnlyAsync(reservationItem, ct);

        if (slotsToRemove.Any())
            await DeleteSlotsAsync(slotsToRemove, tableKey, date, ct);
    }

    private async Task UpdateDifferentTableOrDayAsync(
        Dictionary<string, AttributeValue> reservationItem,
        List<string> newSlots,
        List<string> oldSlots,
        string targetTableKey,
        string oldTableKey,
        DateOnly newDate,
        DateOnly oldDate,
        long ttl,
        CancellationToken ct)
    {
        await AddSlotsWithOverlapCheckAsync(reservationItem, newSlots, targetTableKey, newDate, ttl, ct);
        await DeleteSlotsAsync(oldSlots, oldTableKey, oldDate, ct);
    }

    private async Task AddSlotsWithOverlapCheckAsync(
        Dictionary<string, AttributeValue> reservationItem,
        List<string> slotsToAdd,
        string tableKey,
        DateOnly date,
        long ttl,
        CancellationToken ct)
    {
        var exprValues = slotsToAdd
            .Select((s, i) => (Key: $":ns{i}", Value: s))
            .ToDictionary(x => x.Key, x => new AttributeValue { S = x.Value });

        exprValues[":slotsToAdd"] = new AttributeValue { SS = slotsToAdd };
        exprValues[":ttl"] = new AttributeValue { N = ttl.ToString() };

        var overlapCondition = string.Join(" AND ",
            slotsToAdd.Select((_, i) => $"NOT contains(reservedSlots, :ns{i})"));

        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Put = new Put
                {
                    TableName           = "Reservations",
                    Item                = reservationItem,
                    ConditionExpression = "attribute_exists(id)"
                }
            },
            new()
            {
                Update = new Update
                {
                    TableName = "TableDays",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["tableKey"] = new() { S = tableKey },
                        ["date"]     = new() { S = date.ToString("yyyy-MM-dd") }
                    },
                    UpdateExpression          = "ADD reservedSlots :slotsToAdd SET #ttl = if_not_exists(#ttl, :ttl)",
                    ExpressionAttributeNames  = new Dictionary<string, string> { ["#ttl"] = "ttl" },
                    ConditionExpression       = overlapCondition,
                    ExpressionAttributeValues = exprValues
                }
            }
        };

        try
        {
            await _dynamoDb.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = transactItems }, ct);
        }
        catch (TransactionCanceledException)
        {
            throw new BusinessException("Update failed: requested time slots are already taken.");
        }
    }

    private async Task UpdateReservationItemOnlyAsync(
        Dictionary<string, AttributeValue> reservationItem,
        CancellationToken ct)
    {
        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Put = new Put
                {
                    TableName           = "Reservations",
                    Item                = reservationItem,
                    ConditionExpression = "attribute_exists(id)"
                }
            }
        };

        await _dynamoDb.TransactWriteItemsAsync(
            new TransactWriteItemsRequest { TransactItems = transactItems }, ct);
    }

    private async Task DeleteSlotsAsync(
        List<string> slotsToRemove,
        string tableKey,
        DateOnly date,
        CancellationToken ct)
    {
        var transactItems = new List<TransactWriteItem>
        {
            new()
            {
                Update = new Update
                {
                    TableName = "TableDays",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["tableKey"] = new() { S = tableKey },
                        ["date"]     = new() { S = date.ToString("yyyy-MM-dd") }
                    },
                    UpdateExpression = "DELETE reservedSlots :slotsToRemove",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":slotsToRemove"] = new AttributeValue { SS = slotsToRemove }
                    }
                }
            }
        };

        await _dynamoDb.TransactWriteItemsAsync(
            new TransactWriteItemsRequest { TransactItems = transactItems }, ct);
    }

    private static long ComputeTtl(DateOnly date) =>
        new DateTimeOffset(date.AddDays(2).ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();
}