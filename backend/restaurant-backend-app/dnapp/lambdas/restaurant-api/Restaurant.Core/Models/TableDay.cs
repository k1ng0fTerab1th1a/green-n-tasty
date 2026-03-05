using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("TableDay")]
public class TableDay
{
    [DynamoDBHashKey("tableKey")]
    public string TableKey { get; set; } = null!;

    [DynamoDBRangeKey("date")]
    public string Date { get; set; } = null!;

    [DynamoDBProperty("reservedSlots")]
    public HashSet<string> ReservedSlots { get; set; } = new HashSet<string>();

    [DynamoDBProperty("ttl")]
    public long Ttl { get; set; }
}
