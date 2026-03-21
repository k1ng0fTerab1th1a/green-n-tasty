using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Models;

namespace Restaurant.Reports.Models;

[DynamoDBTable("Reports")]
public sealed class ReportEntry
{
    [DynamoDBHashKey("reservationId")]
    public string ReservationId { get; set; } = null!;

    [DynamoDBProperty("locationId")]
    [DynamoDBGlobalSecondaryIndexHashKey("locationId-completedAt-index")]
    public string LocationId { get; set; } = null!;

    [DynamoDBProperty("completedAt")]
    [DynamoDBGlobalSecondaryIndexRangeKey("locationId-completedAt-index")]
    public string CompletedAt { get; set; } = null!;

    [DynamoDBProperty("durationMinutes")]
    public int DurationMinutes { get; set; }

    [DynamoDBProperty("waiterId")]
    public string WaiterId { get; set; } = null!;

    [DynamoDBProperty("orderId")]
    public string? OrderId { get; set; }

    [DynamoDBProperty("totalRevenue")]
    public float TotalRevenue { get; set; }

    [DynamoDBProperty("serviceFeedback")]
    public int? ServiceFeedback { get; set; }

    [DynamoDBProperty("cuisineFeedback")]
    public int? CuisineFeedback { get; set; }

    public ReportEntry(Reservation reservation, Order order, int? serviceFeedback, int? cuisineFeedback)
    {
        var actualStart = reservation.ActualStartTime != null
            ? DateTimeOffset.Parse(reservation.ActualStartTime)
            : DateTimeOffset.Parse(reservation.StartDateTime);

        var actualEnd = reservation.ActualEndTime != null
            ? DateTimeOffset.Parse(reservation.ActualEndTime)
            : DateTimeOffset.Parse(reservation.EndDateTime);

        LocationId = reservation.LocationId;
        CompletedAt = $"{actualEnd}#{reservation.Id}";
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
