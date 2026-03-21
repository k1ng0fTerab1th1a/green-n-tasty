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
    public string Id { get; set; } 

    [DynamoDBProperty("rate")]
    public int Rate { get; set; }

    [DynamoDBProperty("comment")]
    public string Comment { get; set; }

    [DynamoDBProperty("userId")]
    public string UserId { get; set; }

    [DynamoDBProperty("date")]
    public string Date { get; set; }

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; }

    [DynamoDBProperty("locationId#type")]
    public string LocationIdAndType { get; set; }

    [DynamoDBProperty("type")]
    public string Type { get; set; } // "waiter" | "kitchen"
    [DynamoDBProperty("userName")]
    public string UserName { get; set; }
    [DynamoDBProperty("userAvatarUrl")]
    public string UserAvatarUrl { get; set; }
}