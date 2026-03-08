using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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

        public async Task<Reservation?> UpdateReservationAsync(Reservation reservation, List<string> slots, int
            guestCapacity, bool dayDifferent, Table? table = null)
        {
            // With new tableday creation
            if (dayDifferent || table != null)
            {
                
            }
            // edit the old tableday
            else
            {
                
            }
        }
    }
}
