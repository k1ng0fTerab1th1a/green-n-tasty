using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Models;

[DynamoDBTable("Reservations")]
public sealed class Reservation
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = null!;

    [DynamoDBProperty("customerId")]
    [DynamoDBGlobalSecondaryIndexHashKey("customerId-start-index")]
    public string? CustomerId { get; set; }

    [DynamoDBProperty("customerName")]
    public string? CustomerName { get; set; }

    [DynamoDBProperty("waiterId")]
    [DynamoDBGlobalSecondaryIndexHashKey("waiterId-start-index")]
    public string WaiterId { get; set; } = null!;

    [DynamoDBProperty("waiterName")]
    public string WaiterName { get; set; } = null!;

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; } = null!;

    [DynamoDBProperty("locationAddress")]
    public string LocationAddress { get; set; } = null!;

    [DynamoDBProperty("tableNumber")]
    public int TableNumber { get; set; }

    [DynamoDBProperty("tableKey")]
    [DynamoDBGlobalSecondaryIndexHashKey("tableKey-start-index")]
    public string TableKey { get; set; } = null!; // $"{locationId}#{tableNumber}"

    [DynamoDBProperty("startDateTime")]
    [DynamoDBGlobalSecondaryIndexRangeKey(
        "customerId-start-index",
        "tableKey-start-index",
        "waiterId-start-index")]
    public string StartDateTime { get; set; } = null!;

    [DynamoDBProperty("endDateTime")]
    public string EndDateTime { get; set; } = null!;

    [DynamoDBProperty("actualStartTime")]
    public string? ActualStartTime { get; set; }

    [DynamoDBProperty("actualEndTime")]
    public string? ActualEndTime { get; set; }

    [DynamoDBProperty("guestsCount")]
    public int GuestsCount { get; set; }

    [DynamoDBProperty("status", typeof(ReservationStatusConverter))]
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    [DynamoDBProperty("isMealServed")]
    public bool IsMealServed { get; set; }

    [DynamoDBProperty("isCreatedByWaiter")]
    public bool IsCreatedByWaiter { get; set; }

    [DynamoDBProperty("visitorName")]
    public string? VisitorName { get; set; }

    [DynamoDBProperty("dishCount")]
    public int DishCount { get; set; } = 0;

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; } = null!;

    [DynamoDBProperty("updatedAt")]
    public string UpdatedAt { get; set; } = null!;
    [DynamoDBProperty("serviceFeedbackId")]
    public string? ServiceFeedbackId { get; set; }
    [DynamoDBProperty("kitchenFeedbackId")]
    public string? KitchenFeedbackId { get; set; }
    [DynamoDBProperty("secretCode")]
    public string? SecretCode { get; set; }
}