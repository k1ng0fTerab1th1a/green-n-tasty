using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("waiters-list")]
public class WaiterListEntry
{
    [DynamoDBHashKey("email")]
    public required string Email { get; set; }

    [DynamoDBProperty("locationId")]
    public string? LocationId { get; set; }
}
