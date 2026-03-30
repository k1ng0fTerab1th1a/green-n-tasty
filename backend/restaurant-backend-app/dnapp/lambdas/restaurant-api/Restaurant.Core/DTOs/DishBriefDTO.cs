namespace Restaurant.Core.DTOs;

public class DishBriefDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DishType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? Weight { get; set; }
    public string State { get; set; } = "ON";
}

// Dish types:     Appetizer, MainCourse, Desert, Drink, Snack, Special