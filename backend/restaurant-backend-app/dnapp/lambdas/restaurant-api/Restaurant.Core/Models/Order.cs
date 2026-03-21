using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Models;

[DynamoDBTable("Orders")]
public sealed class Order
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = null!;

    [DynamoDBProperty("reservationId")]
    [DynamoDBGlobalSecondaryIndexHashKey("reservationId-index")]
    public string? ReservationId { get; set; }

    [DynamoDBProperty("locationId")]
    public string LocationId { get; set; } = null!;

    [DynamoDBProperty("locationAddress")]
    public string LocationAddress { get; set; } = null!;

    [DynamoDBProperty("waiterId")]
    public string WaiterId { get; set; } = null!;

    [DynamoDBProperty("waiterName")]
    public string WaiterName { get; set; } = null!;

    [DynamoDBProperty("customerId")]
    public string? CustomerId { get; set; }

    [DynamoDBProperty("customerName")]
    public string? CustomerName { get; set; }

    [DynamoDBProperty("visitorName")]
    public string? VisitorName { get; set; }

    [DynamoDBProperty("tableNumber")]
    public int TableNumber { get; set; }

    [DynamoDBProperty("guestsCount")]
    public int GuestsCount { get; set; }

    [DynamoDBProperty("status", typeof(OrderStatusConverter))]
    public OrderStatus Status { get; set; } = OrderStatus.Open;

    [DynamoDBProperty("dishesJson")]
    public string DishesJson { get; set; } = "[]";

    [DynamoDBProperty("totalAmount")]
    public float TotalAmount { get; set; }

    [DynamoDBProperty("createdAt")]
    public string CreatedAt { get; set; } = null!;

    [DynamoDBProperty("completedAt")]
    public string? CompletedAt { get; set; }
}
