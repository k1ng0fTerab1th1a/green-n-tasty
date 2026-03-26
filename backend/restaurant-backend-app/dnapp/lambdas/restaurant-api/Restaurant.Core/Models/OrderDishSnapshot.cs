namespace Restaurant.Core.Models;

public class OrderDishSnapshot
{
    public string DishId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal PriceAtOrder { get; set; }
    public int Quantity { get; set; }
}