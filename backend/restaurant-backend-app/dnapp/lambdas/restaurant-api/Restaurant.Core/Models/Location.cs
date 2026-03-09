using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("Locations")]
public class Location
{
    [DynamoDBHashKey("id")]
    [DynamoDBGlobalSecondaryIndexRangeKey("entityType-index")]
    public string Id { get; set; } = null!;

    [DynamoDBProperty("entityType")]
    [DynamoDBGlobalSecondaryIndexHashKey("entityType-index")]
    public string EntityType { get; set; } = "LOCATION";

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
    public double Rating { get; set; }
}