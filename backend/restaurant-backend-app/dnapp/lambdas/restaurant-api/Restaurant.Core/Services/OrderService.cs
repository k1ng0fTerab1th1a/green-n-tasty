using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class OrderService(
    IOrderRepository _orderRepo,
    IReservationRepository _reservationRepo,
    IDishRepository _dishRepo) : IOrderService
{
    public async Task<Result<Order>> CreateAsyncForReservation(
        string actorId,
        CreateOrderDTO dto,
        CancellationToken ct = default)
    {
        if (dto.Dishes.Count == 0)
            return OrderErrors.NoDishesProvided;

        var normalizedItems = dto.Dishes
            .GroupBy(x => x.DishId, StringComparer.Ordinal)
            .Select(x => new OrderDishItemDTO
            {
                DishId = x.Key,
                Quantity = x.Sum(y => y.Quantity)
            })
            .ToList();

        var reservationResult = await GetReservationForActorAsync(actorId, dto.ReservationId, ct);
        if (reservationResult.IsFailed)
            return Result.Fail<Order>(reservationResult.Errors);

        var reservation = reservationResult.Value;

        if (reservation.Status != ReservationStatus.InProgress)
            return OrderErrors.ReservationStatusNotOrderable;

        var existingOrder = await _orderRepo.GetByReservationIdAsync(dto.ReservationId, ct);
        if (existingOrder is not null)
            return OrderErrors.OrderAlreadyExists;

        var dishIds = normalizedItems.Select(x => x.DishId).Distinct().ToList();
        var dishes = await _dishRepo.GetByIdsAsync(dishIds, ct);

        var notFound = dishIds.Except(dishes.Select(d => d.Id)).FirstOrDefault();
        if (notFound is not null)
            return OrderErrors.DishNotFound(notFound);

        var inactiveDish = dishes.FirstOrDefault(d => !IsDishEnabled(d.State));
        if (inactiveDish is not null)
            return OrderErrors.DishNotAvailable(inactiveDish.Id);

        var dishMap = dishes.ToDictionary(d => d.Id);
        var dishSnapshots = normalizedItems.Select(item =>
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

        var now = DateTimeOffset.UtcNow.ToString("O");

        var order = new Order
        {
            Id = reservation.Id,
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
            TotalAmount = CalculateTotalAmount(dishSnapshots),
            CreatedAt = now,
            CompletedAt = null,
            Version = 1,
            ProcessedOperationIds = []
        };

        var dishCount = dishSnapshots.Sum(d => d.Quantity);

        var created = await _orderRepo.CreateWithReservationUpdateAsync(
            order,
            reservation.Id,
            actorId,
            dishCount,
            now,
            ct);

        if (!created)
        {
            var orderAfterConflict = await _orderRepo.GetByReservationIdAsync(dto.ReservationId, ct);
            if (orderAfterConflict is not null)
                return OrderErrors.OrderAlreadyExists;

            return OrderErrors.ReservationStateConflict;
        }

        return order;
    }

    public async Task<Result<Order>> GetByReservationAsync(
        string actorId,
        string reservationId,
        CancellationToken ct = default)
    {
        var reservationResult = await GetReservationForActorAsync(actorId, reservationId, ct);
        if (reservationResult.IsFailed)
            return Result.Fail<Order>(reservationResult.Errors);

        var order = await _orderRepo.GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return OrderErrors.OrderNotFound;

        order.ProcessedOperationIds ??= [];
        return order;
    }

    public async Task<Result<Order>> AddDishAsync(
        string actorId,
        string reservationId,
        AddDishToOrderDTO dto,
        CancellationToken ct = default)
    {
        var reservationResult = await GetReservationForActorAsync(actorId, reservationId, ct);
        if (reservationResult.IsFailed)
            return Result.Fail<Order>(reservationResult.Errors);

        var reservation = reservationResult.Value;
        if (reservation.Status != ReservationStatus.InProgress)
            return OrderErrors.ReservationStatusNotOrderable;

        var order = await _orderRepo.GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return OrderErrors.OrderNotFound;

        order.ProcessedOperationIds ??= [];

        if (order.ProcessedOperationIds.Contains(dto.OperationId))
            return order;

        if (order.Status != OrderStatus.Open)
            return OrderErrors.OrderNotOpen;

        var dish = await _dishRepo.GetDishByIdAsync(dto.DishId, ct);
        if (dish is null)
            return OrderErrors.DishNotFound(dto.DishId);

        if (!IsDishEnabled(dish.State))
            return OrderErrors.DishNotAvailable(dto.DishId);

        var expectedVersion = order.Version;

        var existingDish = order.Dishes.FirstOrDefault(x => x.DishId == dto.DishId);
        if (existingDish is null)
        {
            order.Dishes.Add(new OrderDishSnapshot
            {
                DishId = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                PhotoUrl = dish.ImageUrl,
                PriceAtOrder = dish.Price,
                Quantity = dto.Quantity
            });
        }
        else
        {
            existingDish.Quantity += dto.Quantity;
        }

        order.TotalAmount = CalculateTotalAmount(order.Dishes);
        order.Version = expectedVersion + 1;
        order.ProcessedOperationIds.Add(dto.OperationId);

        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var updated = await _orderRepo.UpdateWithReservationDishCountAsync(
            order,
            reservation,
            expectedVersion,
            dto.OperationId,
            dto.Quantity,
            ct);

        if (updated)
            return order;

        return await ResolveMutationConflictAsync(reservationId, dto.OperationId, ct);
    }

    public async Task<Result<Order>> DeleteDishAsync(
        string actorId,
        string reservationId,
        DeleteDishFromOrderDTO dto,
        CancellationToken ct = default)
    {
        var reservationResult = await GetReservationForActorAsync(actorId, reservationId, ct);
        if (reservationResult.IsFailed)
            return Result.Fail<Order>(reservationResult.Errors);

        var reservation = reservationResult.Value;
        if (reservation.Status != ReservationStatus.InProgress)
            return OrderErrors.ReservationStatusNotOrderable;

        var order = await _orderRepo.GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return OrderErrors.OrderNotFound;

        order.ProcessedOperationIds ??= [];

        if (order.ProcessedOperationIds.Contains(dto.OperationId))
            return order;

        if (order.Status != OrderStatus.Open)
            return OrderErrors.OrderNotOpen;

        var existingDish = order.Dishes.FirstOrDefault(x => x.DishId == dto.DishId);
        if (existingDish is null)
            return OrderErrors.DishNotFoundInOrder(dto.DishId);

        if (dto.Quantity > existingDish.Quantity)
            return OrderErrors.InvalidDishRemovalQuantity(dto.DishId);

        var expectedVersion = order.Version;

        if (dto.Quantity == existingDish.Quantity)
        {
            order.Dishes.Remove(existingDish);
        }
        else
        {
            existingDish.Quantity -= dto.Quantity;
        }

        order.TotalAmount = CalculateTotalAmount(order.Dishes);
        order.Version = expectedVersion + 1;
        order.ProcessedOperationIds.Add(dto.OperationId);

        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var updated = await _orderRepo.UpdateWithReservationDishCountAsync(
            order,
            reservation,
            expectedVersion,
            dto.OperationId,
            -dto.Quantity,
            ct);

        if (updated)
            return order;

        return await ResolveMutationConflictAsync(reservationId, dto.OperationId, ct);
    }

    public async Task<Result<Order>> CompleteAsync(
        string actorId,
        string reservationId,
        CompleteOrderDTO dto,
        CancellationToken ct = default)
    {
        var reservationResult = await GetReservationForActorAsync(actorId, reservationId, ct);
        if (reservationResult.IsFailed)
            return Result.Fail<Order>(reservationResult.Errors);

        var reservation = reservationResult.Value;
        if ((reservation.Status is not ReservationStatus.InProgress) && !reservationResult.Value.IsMealServed)
            return OrderErrors.ReservationStatusNotCompletable;

        var order = await _orderRepo.GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return OrderErrors.OrderNotFound;

        order.ProcessedOperationIds ??= [];

        if (order.ProcessedOperationIds.Contains(dto.OperationId))
            return order;

        if (order.Status != OrderStatus.Open)
            return OrderErrors.OrderNotOpen;

        var expectedVersion = order.Version;

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTimeOffset.UtcNow.ToString("O");
        order.Version = expectedVersion + 1;
        order.ProcessedOperationIds.Add(dto.OperationId);

        var completed = await _orderRepo.CompleteAsync(
            order,
            actorId,
            expectedVersion,
            dto.OperationId,
            ct);

        if (!completed)
            return await ResolveMutationConflictAsync(reservationId, dto.OperationId, ct);

        await IncrementPopularityAsync(
            order.Dishes
                .GroupBy(x => x.DishId)
                .Select(x => (DishId: x.Key, Quantity: x.Sum(y => y.Quantity))),
            ct);

        return order;
    }

    private static bool IsDishEnabled(string? state)
        => string.Equals(state, "ON", StringComparison.OrdinalIgnoreCase);

    private async Task<Result<Reservation>> GetReservationForActorAsync(
        string actorId,
        string reservationId,
        CancellationToken ct)
    {
        var reservation = await _reservationRepo.GetByIdAsync(reservationId, ct);
        if (reservation is null)
            return Result.Fail<Reservation>(OrderErrors.ReservationNotFound);

        if (!string.Equals(reservation.WaiterId, actorId, StringComparison.Ordinal))
            return Result.Fail<Reservation>(OrderErrors.Forbidden);

        return reservation;
    }

    private async Task<Result<Order>> ResolveMutationConflictAsync(
        string reservationId,
        string operationId,
        CancellationToken ct)
    {
        var currentOrder = await _orderRepo.GetByReservationIdAsync(reservationId, ct);
        if (currentOrder is not null)
        {
            currentOrder.ProcessedOperationIds ??= [];

            if (currentOrder.ProcessedOperationIds.Contains(operationId))
                return currentOrder;
        }

        return OrderErrors.OrderStateConflict;
    }

    private async Task IncrementPopularityAsync(
        IEnumerable<(string DishId, int Quantity)> increments,
        CancellationToken ct)
    {
        foreach (var item in increments)
        {
            if (string.IsNullOrWhiteSpace(item.DishId) || item.Quantity <= 0)
                continue;

            await _dishRepo.IncrementPopularityAsync(item.DishId, item.Quantity, ct);
        }
    }

    private static decimal CalculateTotalAmount(IEnumerable<OrderDishSnapshot> dishes)
        => dishes.Sum(x => x.PriceAtOrder * x.Quantity);
}