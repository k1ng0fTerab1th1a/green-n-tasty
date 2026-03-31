using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

public enum FeedbackType
{
    Waiter,
    Kitchen
}

[DynamoDBTable("Feedbacks")]
public class Feedback
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty("reservationId")]
    [DynamoDBGlobalSecondaryIndexHashKey("reservationId-index")]
    public string ReservationId { get; set; } = string.Empty;

    [DynamoDBProperty("rate")]
    public int Rate { get; set; }

    [DynamoDBProperty("comment")]
    public string Comment { get; set; } = string.Empty;

    [DynamoDBProperty("userId")] 
    public string UserId { get; set; } = string.Empty;

    [DynamoDBProperty("date")] 
    public string Date { get; set; } = string.Empty;

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; } = string.Empty;

    [DynamoDBProperty("locationId#type")]
    public string LocationIdAndType { get; set; } = string.Empty;

    [DynamoDBProperty("type")]
    public string Type { get; set; } = string.Empty; // "waiter" | "kitchen"
    [DynamoDBProperty("userName")]
    public string UserName { get; set; } = string.Empty;
    [DynamoDBProperty("userAvatarUrl")]
    public string UserAvatarUrl { get; set; } = string.Empty;
}