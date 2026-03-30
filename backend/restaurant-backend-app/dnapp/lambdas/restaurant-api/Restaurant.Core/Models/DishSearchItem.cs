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

    [DynamoDBProperty("dishType")]
    public string DishType { get; set; } = string.Empty;

    [DynamoDBProperty("price")]
    public decimal Price { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty("weight")]
    public int? Weight { get; set; }

    [DynamoDBProperty("state")]
    public string State { get; set; } = "ON";
}
