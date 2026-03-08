using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Tables")]
public class Table
{
    [DynamoDBHashKey("locationId")]
    public required string LocationId { get; set; }

    [DynamoDBRangeKey("tableNumber")]
    public required int TableNumber { get; set; }

    [DynamoDBProperty("locationAddress")]
    public required string LocationAddress { get; set; }

    [DynamoDBProperty("capacity")]
    public required int Capacity { get; set; }
}
