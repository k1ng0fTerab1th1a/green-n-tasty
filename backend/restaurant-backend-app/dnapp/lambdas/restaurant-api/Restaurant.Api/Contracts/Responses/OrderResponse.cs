using Restaurant.Core.Models;

namespace Restaurant.Api.Contracts.Responses;

public sealed class OrderResponse
{
    public string Id { get; set; } = null!;
    public string? ReservationId { get; set; }
    public string LocationId { get; set; } = null!;
    public string LocationAddress { get; set; } = null!;
    public string WaiterName { get; set; } = null!;
    public string? CustomerName { get; set; }
    public string? VisitorName { get; set; }
    public int TableNumber { get; set; }
    public int GuestsCount { get; set; }
    public string Status { get; set; } = null!;
    public List<OrderDishSnapshot> Dishes { get; set; } = [];
    public float TotalAmount { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string? CompletedAt { get; set; }
}
