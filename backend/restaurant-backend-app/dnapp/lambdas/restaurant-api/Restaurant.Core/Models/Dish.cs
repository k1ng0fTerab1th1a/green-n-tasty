using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Core.Models;

[DynamoDBTable("Dishes")]
public class Dish
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; }

    [DynamoDBProperty("name")]
    public string Name { get; set; }

    [DynamoDBProperty("dishType")]
    public string DishType { get; set; }

    [DynamoDBProperty("price")]
    public float Price { get; set; }

    [DynamoDBProperty("state")]
    public string State { get; set; } = "ON";

    [DynamoDBProperty("description")]
    public string? Description { get; set; }

    [DynamoDBProperty("imageUrl")]
    public string? ImageUrl { get; set; }


    [DynamoDBProperty("weight")]
    public int? Weight { get; set; }

    [DynamoDBProperty("calories")]
    public int? Calories { get; set; }

    [DynamoDBProperty("proteins")]
    public float? Proteins { get; set; }

    [DynamoDBProperty("fats")]
    public float? Fats { get; set; }

    [DynamoDBProperty("carbohydrates")]
    public float? Carbohydrates { get; set; }

    [DynamoDBProperty("vitamins")]
    public string? Vitamins { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("PopularDishesIndex")]
    [DynamoDBProperty("popularityFlag")]
    public string? PopularityFlag { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("SpecialityIndex")]
    [DynamoDBProperty("specialityForLocation")]
    public string? SpecialityForLocation { get; set; }
}