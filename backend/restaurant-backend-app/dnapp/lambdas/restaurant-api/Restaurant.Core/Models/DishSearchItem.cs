using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("DishSearch")]
public sealed class DishSearchItem
{
    [DynamoDBHashKey("token")]
    public string Token { get; set; } = null!;

    [DynamoDBRangeKey("dishId")]
    public string DishId { get; set; } = null!;

    [DynamoDBProperty("nameNormalized")]
    public string NameNormalized { get; set; } = null!;

    [DynamoDBProperty("name")]
    public string Name { get; set; } = null!;
}
