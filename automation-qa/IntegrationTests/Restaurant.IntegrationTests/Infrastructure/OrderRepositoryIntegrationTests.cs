using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public sealed class OrderRepositoryIntegrationTests
{
    private readonly DynamoDBContext _context;
    private readonly OrderRepository _repo;

    public OrderRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo = new OrderRepository(fixture.Context, fixture.Client);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistOrder()
    {
        var order = BuildOrder();

        await _repo.CreateAsync(order);

        var loaded = await _context.LoadAsync<Order>(order.Id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(order.Id);
        loaded.ReservationId.Should().Be(order.ReservationId);
        loaded.TotalAmount.Should().Be(order.TotalAmount);
    }

    [Fact]
    public async Task CreateWithReservationUpdateAsync_WhenConditionsMet_ShouldCreateOrderAndUpdateReservation()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        await _context.SaveAsync(reservation);

        var order = BuildOrder(reservationId: reservation.Id, waiterId: reservation.WaiterId);
        var updatedAt = DateTimeOffset.UtcNow.ToString("O");

        var ok = await _repo.CreateWithReservationUpdateAsync(
            order,
            reservation.Id,
            reservation.WaiterId,
            dishCount: 3,
            updatedAt: updatedAt,
            ct: default);

        ok.Should().BeTrue();

        var loadedOrder = await _context.LoadAsync<Order>(order.Id);
        loadedOrder.Should().NotBeNull();
        loadedOrder!.ReservationId.Should().Be(reservation.Id);

        var loadedReservation = await _context.LoadAsync<Reservation>(reservation.Id);
        loadedReservation.Should().NotBeNull();
        loadedReservation!.DishCount.Should().Be(3);
        loadedReservation.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task CreateWithReservationUpdateAsync_WhenDishCountAlreadySet_ShouldReturnFalseAndNotCreateOrder()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 2);
        await _context.SaveAsync(reservation);

        var order = BuildOrder(reservationId: reservation.Id, waiterId: reservation.WaiterId);

        var ok = await _repo.CreateWithReservationUpdateAsync(
            order,
            reservation.Id,
            reservation.WaiterId,
            dishCount: 4,
            updatedAt: DateTimeOffset.UtcNow.ToString("O"),
            ct: default);

        ok.Should().BeFalse();

        var loadedOrder = await _context.LoadAsync<Order>(order.Id);
        loadedOrder.Should().BeNull();

        var loadedReservation = await _context.LoadAsync<Reservation>(reservation.Id);
        loadedReservation.Should().NotBeNull();
        loadedReservation!.DishCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateWithReservationUpdateAsync_WhenWaiterMismatch_ShouldReturnFalseAndNotCreateOrder()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        await _context.SaveAsync(reservation);

        var order = BuildOrder(reservationId: reservation.Id, waiterId: "waiter-2");

        var ok = await _repo.CreateWithReservationUpdateAsync(
            order,
            reservation.Id,
            waiterId: "waiter-2",
            dishCount: 1,
            updatedAt: DateTimeOffset.UtcNow.ToString("O"),
            ct: default);

        ok.Should().BeFalse();

        var loadedOrder = await _context.LoadAsync<Order>(order.Id);
        loadedOrder.Should().BeNull();

        var loadedReservation = await _context.LoadAsync<Reservation>(reservation.Id);
        loadedReservation.Should().NotBeNull();
        loadedReservation!.DishCount.Should().Be(0);
    }
    

    [Fact]
    public async Task CompleteOnReservationFinishIfOpenAsync_WhenOrderIsNotOpen_ShouldReturnTrue()
    {
        var order = BuildOrder();
        order.Status = OrderStatus.Completed;
        await _context.SaveAsync(order);

        var result = await _repo.CompleteOnReservationFinishIfOpenAsync(order.ReservationId, order.WaiterId, DateTimeOffset.UtcNow.ToString("O"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_WhenConditionsMet_ShouldCompleteOrder()
    {
        var order = BuildOrder();
        await _context.SaveAsync(order);
        
        order.CompletedAt = DateTimeOffset.UtcNow.ToString("O");

        var result = await _repo.CompleteAsync(order, order.WaiterId, order.Version, Guid.NewGuid().ToString(), CancellationToken.None);

        result.Should().BeTrue();

        var updatedOrder = await _context.LoadAsync<Order>(order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task CompleteAsync_WhenConditionsNotMet_ShouldReturnFalse()
    {
        var order = BuildOrder();
        await _context.SaveAsync(order);
        
        order.CompletedAt = DateTimeOffset.UtcNow.ToString("O");

        var result = await _repo.CompleteAsync(order, "wrong-waiter-id", order.Version, Guid.NewGuid().ToString(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByReservationIdAsync_WhenReservationDoesNotExist_ShouldReturnNull()
    {
        var result = await _repo.GetByReservationIdAsync(Guid.NewGuid().ToString("N"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWithReservationDishCountAsync_WhenConditionsNotMet_ShouldReturnFalse()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        await _context.SaveAsync(reservation);

        var order = BuildOrder(reservationId: reservation.Id, waiterId: reservation.WaiterId);
        await _context.SaveAsync(order);

        var result = await _repo.UpdateWithReservationDishCountAsync(order, order.Version, Guid.NewGuid().ToString(), -1, DateTimeOffset.UtcNow.ToString("O"), CancellationToken.None);

        result.Should().BeFalse();
    }

    private static Reservation BuildReservation(string waiterId, ReservationStatus status, int dishCount)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        return new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            CustomerName = "Customer",
            WaiterId = waiterId,
            WaiterName = "Waiter",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = now.AddMinutes(-30).ToString("O"),
            EndDateTime = now.AddMinutes(30).ToString("O"),
            GuestsCount = 2,
            Status = status,
            DishCount = dishCount,
            CreatedAt = now.AddHours(-1).ToString("O"),
            UpdatedAt = now.ToString("O")
        };
    }

    private static Order BuildOrder(string? reservationId = null, string waiterId = "waiter-1")
    {
        var now = DateTimeOffset.UtcNow;

        return new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            ReservationId = reservationId ?? Guid.NewGuid().ToString("N"),
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            WaiterId = waiterId,
            WaiterName = "Waiter",
            CustomerId = "customer-1",
            CustomerName = "Customer",
            VisitorName = null,
            TableNumber = 3,
            GuestsCount = 2,
            Status = OrderStatus.Open,
            Dishes = new List<OrderDishSnapshot>(),
            TotalAmount = 42.5m,
            CreatedAt = now.ToString("O"),
            CompletedAt = null
        };
    }
}