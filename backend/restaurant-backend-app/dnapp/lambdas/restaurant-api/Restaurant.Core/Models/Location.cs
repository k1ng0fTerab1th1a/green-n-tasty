using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("Locations")]
public class Location
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = null!;

    [DynamoDBProperty("address")]
    public string Address { get; set; } = null!;

    [DynamoDBProperty("timeZone")]
    public string TimeZone { get; set; } = "Asia/Tbilisi";

    [DynamoDBProperty("openTime")]
    public string OpenTime { get; set; } = "10:00";

    [DynamoDBProperty("closeTime")]
    public string CloseTime { get; set; } = "22:00";

    [DynamoDBProperty("description")]
    public string Description { get; set; } = null!;

    [DynamoDBProperty("averageOccupancy")]
    public double AverageOccupancy { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string ImageUrl { get; set; } = null!;

    [DynamoDBProperty("totalCapacity")]
    public int TotalCapacity { get; set; }

    [DynamoDBProperty("rating")]
    public int TotalRating { get; set; } = 0;
    [DynamoDBProperty("feedbacksAmount")]
    public int FeedbacksAmount { get; set; } = 0;
}