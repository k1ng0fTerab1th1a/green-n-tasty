using Restaurant.Core.Models;

namespace Restaurant.Core.DTOs;

public class ReceiptDTO
{
    public required string ReservationId { get; set; }
    public required string GuestName { get; set; }
    public required bool GuestIsVisitor { get; set; }
    public string? SecretLink { get; set; }
    public required string WaiterName { get; set; }
    public required string LocationAddress { get; set; }
    public required int TableNumber { get; set; }
    public required int GuestsNumber { get; set; }
    public required List<OrderDishSnapshot> Dishes { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string ActualStartTime { get; set; }
    public required string ActualEndTime { get; set; }
}
