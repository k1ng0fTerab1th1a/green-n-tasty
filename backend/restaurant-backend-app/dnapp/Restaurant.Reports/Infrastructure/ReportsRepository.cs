using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Models;
using Restaurant.Reports.Domain.Entities;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Restaurant.Reports.Infrastructure;

public class ReportsRepository : IReportsRepository
{
    private readonly IDynamoDBContext _dbContext;
    private readonly IAmazonDynamoDB _client;

    public ReportsRepository(IAmazonDynamoDB client, IDynamoDBContext dbContext)
    {
        _client = client;
        _dbContext = dbContext;
    }

    public ReportsRepository()
    {
        _client = new AmazonDynamoDBClient();
        _dbContext = new DynamoDBContext(_client);
    }

    public async Task<Reservation?> GetReservationAsync(string reservationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reservationId))
            return null;

        var reservation = await _dbContext.LoadAsync<Reservation>(reservationId, ct);

        return reservation;
    }

    public async Task<Order?> GetOrderByReservationAsync(string reservationId, CancellationToken ct)
    {
        var query = _dbContext.QueryAsync<Order>(
            reservationId,
            new DynamoDBOperationConfig
            {
                IndexName = "reservationId-index"
            }
        );

        var results = await query.GetRemainingAsync(ct);
        return results.FirstOrDefault();
    }

    public async Task<List<Feedback>> GetFeedbacksByReservationAsync(string reservationId, CancellationToken ct)
    {
        var query = _dbContext.QueryAsync<Feedback>(
            reservationId,
            new DynamoDBOperationConfig
            {
                IndexName = "reservationId-index"
            }
        );

        return await query.GetRemainingAsync(ct);
    }

    public async Task SaveReportEntryAsync(ReportEntry reportEntry, CancellationToken ct)
    {
        await _dbContext.SaveAsync(reportEntry, ct);
    }

    public async Task UpdateReportFeedbackAsync(string reservationId, int? serviceFeedback, int? cuisineFeedback, CancellationToken ct)
    {
        var entry = await _dbContext.LoadAsync<ReportEntry>(reservationId, ct);
        if (entry == null) return;

        if (serviceFeedback.HasValue)
            entry.ServiceFeedback = serviceFeedback;

        if (cuisineFeedback.HasValue)
            entry.CuisineFeedback = cuisineFeedback;

        await _dbContext.SaveAsync(entry, ct);
    }

    public async IAsyncEnumerable<ReportEntry> QueryReportsByDateAsync(DateTime date, DateTime from, DateTime to, [EnumeratorCancellation] CancellationToken ct)
    {
        var request = new QueryRequest
        {
            TableName = "Reports",
            IndexName = "date-completedAt-index",

            KeyConditionExpression = "#d = :date AND completedAt BETWEEN :from AND :to",

            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "date"
            },

            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":date"] = new AttributeValue { S = date.ToString("yyyy-MM-dd") },
                [":from"] = new AttributeValue { S = from.ToString("o") },
                [":to"] = new AttributeValue { S = to.ToString("o") }
            }
        };

        QueryResponse response;

        do
        {
            response = await _client.QueryAsync(request, ct);

            foreach (var item in response.Items)
            {
                yield return MapToReportEntry(item);
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;

        } while (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0);
    }
    public async Task<Dictionary<string, Location>> GetAllLocationsAsync(CancellationToken ct)
    {
        var search = _dbContext.ScanAsync<Location>(new List<ScanCondition>());
        var locations = await search.GetRemainingAsync(ct);
        return locations.ToDictionary(l => l.Id);
    }

    public async Task<Dictionary<string, User>> GetAllWaitersAsync(CancellationToken ct)
    {
        var request = new QueryRequest
        {
            TableName = "Users",
            IndexName = "waiterFlag-index",
            KeyConditionExpression = "waiterFlag = :flag",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":flag"] = new AttributeValue { S = "1" }
            }
        };

        var users = new Dictionary<string, User>();
        QueryResponse response;

        do
        {
            response = await _client.QueryAsync(request, ct);

            foreach (var item in response.Items)
            {
                var user = new User
                {
                    UserId = item["userId"].S,
                    FirstName = item.TryGetValue("firstName", out var fn) ? fn.S : "",
                    LastName = item.TryGetValue("lastName", out var ln) ? ln.S : "",
                    Email = item.TryGetValue("email", out var em) ? em.S : ""
                };
                users[user.UserId] = user;
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;
        } while (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0);

        return users;
    }
    public async Task<Dictionary<string, int>> GetWaiterShiftCountsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");

        var request = new ScanRequest
        {
            TableName = "WaiterSchedule",
            FilterExpression = "#d BETWEEN :from AND :to",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "date"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":from"] = new AttributeValue { S = fromStr },
                [":to"] = new AttributeValue { S = toStr }
            }
        };

        var waiterDates = new Dictionary<string, HashSet<string>>();

        ScanResponse response;
        do
        {
            response = await _client.ScanAsync(request, ct);

            foreach (var item in response.Items)
            {
                var waiterId = item["waiterId"].S;
                var date = item["date"].S;

                if (!waiterDates.TryGetValue(waiterId, out var dates))
                {
                    dates = new HashSet<string>();
                    waiterDates[waiterId] = dates;
                }

                dates.Add(date);
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;
        } while (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0);

        return waiterDates.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }

    private ReportEntry MapToReportEntry(Dictionary<string, AttributeValue> item)
    {
        return new ReportEntry
        {
            ReservationId = item["reservationId"].S,
            LocationId = item["locationId"].S,
            CompletedAt = item["completedAt"].S,
            Date = item["date"].S,
            WaiterId = item["waiterId"].S,
            TotalRevenue = decimal.Parse(item["totalRevenue"].N, CultureInfo.InvariantCulture),
            DurationMinutes = item.TryGetValue("durationMinutes", out var dur)
                ? int.Parse(dur.N, CultureInfo.InvariantCulture)
                : 0,

            ServiceFeedback = item.TryGetValue("serviceFeedback", out var sf)
                ? int.Parse(sf.N, CultureInfo.InvariantCulture)
                : null,

            CuisineFeedback = item.TryGetValue("cuisineFeedback", out var cf)
                ? int.Parse(cf.N, CultureInfo.InvariantCulture)
                : null
        };
    }
}