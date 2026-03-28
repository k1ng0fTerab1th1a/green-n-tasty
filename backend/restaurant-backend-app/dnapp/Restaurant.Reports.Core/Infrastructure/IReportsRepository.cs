using Restaurant.Core.Models;
using Restaurant.Reports.Domain.Entities;

namespace Restaurant.Reports.Infrastructure;

public interface IReportsRepository
{
    Task<Reservation?> GetReservationAsync(string reservationId, CancellationToken ct);
    Task<Order?> GetOrderByReservationAsync(string reservationId, CancellationToken ct);
    Task<List<Feedback>> GetFeedbacksByReservationAsync(string reservationId, CancellationToken ct);
    Task SaveReportEntryAsync(ReportEntry reportEntry, CancellationToken ct);
    Task UpdateReportFeedbackAsync(string reservationId, int? serviceFeedback, int? cuisineFeedback, CancellationToken ct);
    IAsyncEnumerable<ReportEntry> QueryReportsByDateAsync(DateTime date, DateTime from, DateTime to, CancellationToken ct);
    Task<Dictionary<string, Location>> GetAllLocationsAsync(CancellationToken ct);
    Task<Dictionary<string, User>> GetAllWaitersAsync(CancellationToken ct);
    Task<Dictionary<string, int>> GetWaiterShiftCountsAsync(DateTime from, DateTime to, CancellationToken ct);
}
