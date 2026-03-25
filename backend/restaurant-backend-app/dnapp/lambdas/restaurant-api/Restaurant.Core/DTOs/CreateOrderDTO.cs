using System.ComponentModel.DataAnnotations;

namespace Restaurant.Core.DTOs;

public sealed class CreateOrderDTO
{
    [Required]
    public string ReservationId { get; set; } = null!;

    [Required]
    [MinLength(1, ErrorMessage = "At least one dish is required.")]
    public List<OrderDishItemDTO> Dishes { get; set; } = [];
}

public sealed class OrderDishItemDTO
{
    [Required]
    public string DishId { get; set; } = null!;

    [Range(1, 100)]
    public int Quantity { get; set; }
}