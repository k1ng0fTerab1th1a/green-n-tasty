using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Tables")]
public class Table
{
    [DynamoDBHashKey("tableId")]
    public string TableId { get; set; }

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; }

    [DynamoDBProperty("tableNumber")]
    public int TableNumber { get; set; }

    [DynamoDBProperty("capacity")]
    public int Capacity { get; set; }
}