using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;


namespace Restaurant.Infrastructure.Repositories
{
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
            bool isDayDifferent,
            Table? newTable = null,
            CancellationToken ct = default)
        {
            var oldTableKey = reservation.TableKey;
            var oldDate     = DateOnly.Parse(reservation.StartDateTime[..10]);
            var newDate     = DateOnly.Parse(reservation.StartDateTime[..10]);
            var ttl = new DateTimeOffset(newDate.AddDays(2).ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();

            var reservationItem = _context.ToDocument(reservation).ToAttributeMap();

            // ── CASE 1: Same table, same day ────────────────────────────────────────
            // Single transaction: swap old slots for new ones + update reservation
            if (!isDayDifferent && newTable == null)
            {
                var exprValues = newSlots
                    .Select((s, i) => (Key: $":ns{i}", Value: s))
                    .ToDictionary(x => x.Key, x => new AttributeValue { S = x.Value });

                exprValues[":newSlots"] = new AttributeValue { SS = newSlots };
                exprValues[":oldSlots"] = new AttributeValue { SS = oldSlots };

                var conditionExpression = string.Join(" AND ",
                    newSlots.Select((_, i) => $"NOT contains(reservedSlots, :ns{i})"));

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
                                ["tableKey"] = new() { S = oldTableKey },
                                ["date"]     = new() { S = oldDate.ToString("yyyy-MM-dd") }
                            },
                            UpdateExpression          = "DELETE reservedSlots :oldSlots ADD reservedSlots :newSlots",
                            ConditionExpression       = conditionExpression,
                            ExpressionAttributeValues = exprValues
                        }
                    }
                };

                try
                {
                    await _dynamoDb.TransactWriteItemsAsync(
                        new TransactWriteItemsRequest { TransactItems = transactItems }, ct);

                    return reservation;
                }
                catch (TransactionCanceledException ex)
                {
                    throw new BusinessException("Update failed: requested time slots are already taken." + ex);
                }
            }

            // ── CASE 2: Different day or different table ─────────────────────────────
            // Transaction 1: write new reservation + add new slots to new TableDay
            // Transaction 2: remove old slots from old TableDay
            // ── CASE 2: Different day or different table ─────────────────────────────
            var targetTableKey = newTable != null ? reservation.TableKey : oldTableKey;

            var newSlotExprValues = newSlots
                .Select((s, i) => (Key: $":ns{i}", Value: s))
                .ToDictionary(x => x.Key, x => new AttributeValue { S = x.Value });

            newSlotExprValues[":newSlots"] = new AttributeValue { SS = newSlots };
            newSlotExprValues[":ttl"]      = new AttributeValue { N = ttl.ToString() };

            var newSlotCondition = string.Join(" AND ",
                newSlots.Select((_, i) => $"NOT contains(reservedSlots, :ns{i})"));

            // Transaction 1: write updated reservation + add new slots to new TableDay
            // SET #ttl = if_not_exists(...) creates the TableDay item if it doesn't exist yet
            var addTransactItems = new List<TransactWriteItem>
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
                            ["tableKey"] = new() { S = targetTableKey },
                            ["date"]     = new() { S = newDate.ToString("yyyy-MM-dd") }
                        },
                        UpdateExpression = "ADD reservedSlots :newSlots SET #ttl = if_not_exists(#ttl, :ttl)",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#ttl"] = "ttl"
                        },
                        ConditionExpression       = newSlotCondition,
                        ExpressionAttributeValues = newSlotExprValues
                    }
                }
            };

            try
            {
                await _dynamoDb.TransactWriteItemsAsync(
                    new TransactWriteItemsRequest { TransactItems = addTransactItems }, ct);
            }
            catch (TransactionCanceledException ex)
            {
                throw new BusinessException("Update failed: requested time slots are already taken." + ex);
            }

            // Transaction 2: remove old slots from old TableDay
            // Only runs after Transaction 1 succeeds
            var removeTransactItems = new List<TransactWriteItem>
            {
                new()
                {
                    Update = new Update
                    {
                        TableName = "TableDays",
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["tableKey"] = new() { S = oldTableKey },
                            ["date"]     = new() { S = oldDate.ToString("yyyy-MM-dd") }
                        },
                        UpdateExpression = "DELETE reservedSlots :oldSlots",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":oldSlots"] = new AttributeValue { SS = oldSlots }
                        }
                    }
                }
            };

            await _dynamoDb.TransactWriteItemsAsync(
                new TransactWriteItemsRequest { TransactItems = removeTransactItems }, ct);

            return reservation;
        }
    }
}
