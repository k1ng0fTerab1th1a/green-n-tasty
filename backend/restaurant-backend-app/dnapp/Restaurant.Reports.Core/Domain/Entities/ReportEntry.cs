using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Restaurant.Core.Models;
using System.Globalization;

namespace Restaurant.Reports.Domain.Entities;

[DynamoDBTable("Reports")]
public sealed class ReportEntry
{
    [DynamoDBHashKey("reservationId")]
    public string ReservationId { get; set; } = null!;

    [DynamoDBProperty("locationId")]
    [DynamoDBGlobalSecondaryIndexHashKey("locationId-completedAt-index")]
    public string LocationId { get; set; } = null!;

    [DynamoDBProperty("date")]
    [DynamoDBGlobalSecondaryIndexHashKey("date-completedAt-index")]
    public string Date { get; set; } = null!;

    [DynamoDBProperty("completedAt")]
    [DynamoDBGlobalSecondaryIndexRangeKey("locationId-completedAt-index")]
    [DynamoDBGlobalSecondaryIndexHashKey("date-completedAt-index")]
    public string CompletedAt { get; set; } = null!;

    [DynamoDBProperty("durationMinutes")]
    public int DurationMinutes { get; set; }

    [DynamoDBProperty("waiterId")]
    public string WaiterId { get; set; } = null!;

    [DynamoDBProperty("orderId")]
    public string? OrderId { get; set; }

    [DynamoDBProperty("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [DynamoDBProperty("serviceFeedback")]
    public int? ServiceFeedback { get; set; }

    [DynamoDBProperty("cuisineFeedback")]
    public int? CuisineFeedback { get; set; }

    public ReportEntry(Reservation reservation, Order? order, int? serviceFeedback, int? cuisineFeedback)
    {
        var actualStart = DateTimeOffset.Parse(
            reservation.ActualStartTime ?? reservation.StartDateTime,
            CultureInfo.InvariantCulture);

        var actualEnd = DateTimeOffset.Parse(
            reservation.ActualEndTime ?? reservation.EndDateTime,
            CultureInfo.InvariantCulture);

        LocationId = reservation.LocationId;
        CompletedAt = $"{actualEnd:o}#{reservation.Id}";
        Date = actualEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ReservationId = reservation.Id;
        DurationMinutes = (int)(actualEnd - actualStart).TotalMinutes;
        WaiterId = reservation.WaiterId;
        OrderId = order?.Id;
        TotalRevenue = order?.TotalAmount ?? 0;
        ServiceFeedback = serviceFeedback;
        CuisineFeedback = cuisineFeedback;
    }

    public ReportEntry() { }
}
