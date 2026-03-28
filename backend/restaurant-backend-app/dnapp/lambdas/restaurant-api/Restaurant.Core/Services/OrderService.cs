using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using System.Text.Json;

namespace Restaurant.Core.Services;

public class OrderService(IOrderRepository _orderRepo, IReservationRepository _reservationRepo, IDishRepository _dishRepo) : IOrderService
{
    public async Task<Result<Order>> CreateAsyncForReservation(string actorId, CreateOrderDTO dto, CancellationToken ct = default)
    {
        if (dto.Dishes.Count == 0) 
            return OrderErrors.NoDishesProvided;

        var reservation = await _reservationRepo.GetByIdAsync(dto.ReservationId, ct);
        if (reservation is null) 
            return OrderErrors.ReservationNotFound;

        if (!string.Equals(reservation.WaiterId, actorId, StringComparison.Ordinal))
            return OrderErrors.Forbidden;

        if (reservation.DishCount > 0)
            return OrderErrors.OrderAlreadyExists;

        if(reservation.Status != ReservationStatus.InProgress) 
            return OrderErrors.ReservationStatusNotOrderable;

        var dishIds = dto.Dishes.Select(d => d.DishId).ToList();
        var dishes = await _dishRepo.GetByIdsAsync(dishIds, ct);

        var notFound = dishIds.Except(dishes.Select(d => d.Id)).FirstOrDefault();
        if (notFound is not null) 
            return OrderErrors.DishNotFound(notFound);

        var inactiveDish = dishes.FirstOrDefault(d => d.State != "ON");
        if (inactiveDish is not null) 
            return OrderErrors.DishNotAvailable(inactiveDish.Id);

        var dishMap = dishes.ToDictionary(d => d.Id);
        var dishSnapshots = dto.Dishes.Select(item =>
        {
            var dish = dishMap[item.DishId];
            return new OrderDishSnapshot
            {
                DishId = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                PhotoUrl = dish.ImageUrl,
                PriceAtOrder = dish.Price,
                Quantity = item.Quantity
            };
        }).ToList();

        var totalAmount = dishSnapshots.Sum(d => d.PriceAtOrder * d.Quantity);

        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            ReservationId = reservation.Id,
            LocationId = reservation.LocationId,
            LocationAddress = reservation.LocationAddress,
            WaiterId = reservation.WaiterId,
            WaiterName = reservation.WaiterName,
            CustomerId = reservation.CustomerId,
            CustomerName = reservation.CustomerName,
            VisitorName = reservation.VisitorName,
            TableNumber = reservation.TableNumber,
            GuestsCount = reservation.GuestsCount,
            Status = OrderStatus.Open,
            Dishes = dishSnapshots,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CompletedAt = null
        };

        var dishCount = dishSnapshots.Sum(d => d.Quantity);
        var updatedAt = DateTime.UtcNow.ToString("o");

        var created = await _orderRepo.CreateWithReservationUpdateAsync(
            order,
            reservation.Id,
            actorId,
            dishCount,
            updatedAt,
            ct);

        if (!created)
            return OrderErrors.ReservationStateConflict;

        return order;
    }
}
