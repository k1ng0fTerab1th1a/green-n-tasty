using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("WaiterSchedule")]
public sealed class WaiterSchedule
{
    [DynamoDBHashKey("tableKey")]
    public string TableKey { get; set; } = null!; // "locationId#tableNumber"

    [DynamoDBRangeKey("date")]
    public string Date { get; set; } = null!;

    [DynamoDBProperty("waiterId")]
    public string WaiterId { get; set; } = null!;
}
