using Amazon.DynamoDBv2.DataModel;

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