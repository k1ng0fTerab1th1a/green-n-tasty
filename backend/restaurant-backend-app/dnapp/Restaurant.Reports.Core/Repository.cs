using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Models;
using Restaurant.Reports.Models;

namespace Restaurant.Reports;

public class Repository
{
    private readonly IDynamoDBContext _dbContext;

    public Repository()
    {
        var client = new AmazonDynamoDBClient();
        _dbContext = new DynamoDBContextBuilder()
            .WithDynamoDBClient(() => client)
            .Build();
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
            new QueryConfig
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
            new QueryConfig
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
}