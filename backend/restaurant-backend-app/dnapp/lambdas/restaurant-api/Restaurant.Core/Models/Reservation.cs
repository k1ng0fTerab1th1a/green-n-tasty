using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("Reservations")]
public sealed class Reservation
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = null!;

    [DynamoDBProperty("customerId")]
    [DynamoDBGlobalSecondaryIndexHashKey("customerId-start-index")]
    public string CustomerId { get; set; } = null!;

    [DynamoDBProperty("waiterId")]
    [DynamoDBGlobalSecondaryIndexHashKey("waiterId-start-index")]
    public string WaiterId { get; set; } = null!;

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; } = null!;

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

    [DynamoDBProperty("guestsCount")]
    public int GuestsCount { get; set; }

    [DynamoDBProperty("status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; } = null!;

    [DynamoDBProperty("updatedAt")]
    public string UpdatedAt { get; set; } = null!;
}