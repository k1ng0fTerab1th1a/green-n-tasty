using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Infrastructure.Repositories
{
    public sealed class ReservationRepository : IReservationRepository
    {
        private const string CustomerIndex = "customerId-start-index";
        private const string WaiterIndex = "waiterId-start-index";

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
                        UpdateExpression             = "ADD reservedSlots :newSlots",
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
    }
}
