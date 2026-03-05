using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories
{
    public sealed class ReservationRepository : IReservationRepository
    {
        private const string CustomerIndex = "customerId-start-index";
        private const string WaiterIndex = "waiterId-start-index";

        private readonly IDynamoDBContext _context;

        public ReservationRepository(IDynamoDBContext context)
        {
            _context = context;
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
    }
}
