using Amazon.DynamoDBv2.DataModel;

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

    [DynamoDBProperty("user")]
    public string User { get; set; }

    [DynamoDBProperty("date")]
    public string Date { get; set; }

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; }

    [DynamoDBProperty("type")]
    public string Type { get; set; } // "waiter" | "kitchen"
}