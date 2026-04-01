using System.ComponentModel.DataAnnotations;

namespace Restaurant.Core.DTOs;

public sealed class AddDishToOrderDTO
{
    [Required]
    public string OperationId { get; set; } = null!;

    [Required]
    public string DishId { get; set; } = null!;

    [Range(1, 100)]
    public int Quantity { get; set; }
}

public sealed class DeleteDishFromOrderDTO
{
    [Required]
    public string OperationId { get; set; } = null!;

    [Required]
    public string DishId { get; set; } = null!;

    [Range(1, 100)]
    public int Quantity { get; set; }
}

public sealed class CompleteOrderDTO
{
    [Required]
    public string OperationId { get; set; } = null!;
}