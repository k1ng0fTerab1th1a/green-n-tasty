using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.IntegrationTests.Services;

[Collection("DynamoDb collection")]
public sealed class OrderServiceIntegrationTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly OrderService _sut;

    public OrderServiceIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;

        var orderRepo = new OrderRepository(fixture.Context, fixture.Client);
        var reservationRepo = new ReservationRepository(fixture.Context, fixture.Client);
        var dishRepo = new DishRepository(fixture.Context, fixture.Client);

        _sut = new OrderService(orderRepo, reservationRepo, dishRepo);
    }

    [Fact]
    public async Task CreateAsyncForReservation_WhenReservationNotFound_ReturnsNotFoundError()
    {
        var dto = BuildDto($"res-{Guid.NewGuid():N}", ($"dish-{Guid.NewGuid():N}", 1));

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.ReservationNotFound);
    }

    [Fact]
    public async Task CreateAsyncForReservation_WhenDishNotAvailable_ReturnsDishNotAvailableError()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        var unavailableDish = BuildDish(state: "OFF");

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(unavailableDish);

        var dto = BuildDto(reservation.Id, (unavailableDish.Id, 2));

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.DishNotAvailable(unavailableDish.Id));

        var reservationAfter = await _context.LoadAsync<Reservation>(reservation.Id);
        reservationAfter.Should().NotBeNull();
        reservationAfter!.DishCount.Should().Be(0);

        var order = await _context.LoadAsync<Order>(reservation.Id);
        order.Should().BeNull();

        var dishAfter = await _context.LoadAsync<Dish>(unavailableDish.Id);
        dishAfter!.Popularity.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsyncForReservation_WhenValidRequest_PersistsOrder_UpdatesDishCount_AndIncrementsPopularity()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        var dish1 = BuildDish("dish-1", state: "ON", price: 10m, popularity: 2);
        var dish2 = BuildDish("dish-2", state: "ON", price: 25m, popularity: 0);

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(dish1);
        await _context.SaveAsync(dish2);

        var dto = BuildDto(reservation.Id, (dish1.Id, 2), (dish2.Id, 1));

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(reservation.Id);
        result.Value.TotalAmount.Should().Be(45m);

        var reservationAfter = await _context.LoadAsync<Reservation>(reservation.Id);
        reservationAfter!.DishCount.Should().Be(3);

        var order = await _context.LoadAsync<Order>(reservation.Id);
        order.Should().NotBeNull();
        order!.Dishes.Should().HaveCount(2);

        var dish1After = await _context.LoadAsync<Dish>(dish1.Id);
        var dish2After = await _context.LoadAsync<Dish>(dish2.Id);
        dish1After!.Popularity.Should().Be(2);
        dish2After!.Popularity.Should().Be(0);
        dish1After.PopularityFlag.Should().Be("true");
        dish2After.PopularityFlag.Should().BeNull();
    }

    [Fact]
    public async Task AddDishAsync_WhenOperationIdRepeated_DoesNotIncrementDishCountOrPopularityTwice()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 2);
        var existingOrder = BuildOrder(reservation.Id);
        var newDish = BuildDish("dish-2", state: "ON", price: 25m, popularity: 7);

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(existingOrder);
        await _context.SaveAsync(newDish);

        var dto = new AddDishToOrderDTO
        {
            OperationId = "op-add-dup",
            DishId = newDish.Id,
            Quantity = 1
        };

        var first = await _sut.AddDishAsync("waiter-1", reservation.Id, dto, CancellationToken.None);
        var second = await _sut.AddDishAsync("waiter-1", reservation.Id, dto, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        var reservationAfter = await _context.LoadAsync<Reservation>(reservation.Id);
        reservationAfter!.DishCount.Should().Be(3);

        var orderAfter = await _context.LoadAsync<Order>(reservation.Id);
        orderAfter!.ProcessedOperationIds.Should().Contain("op-add-dup");
        orderAfter.Dishes.Should().ContainSingle(x => x.DishId == newDish.Id && x.Quantity == 1);

        var dishAfter = await _context.LoadAsync<Dish>(newDish.Id);
        dishAfter!.Popularity.Should().Be(7);
    }

    [Fact]
    public async Task DeleteDishAsync_WhenValidRequest_UpdatesOrderAndReservationDishCount_ButDoesNotDecreasePopularity()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 2);
        var existingOrder = BuildOrder(reservation.Id);
        var dish = BuildDish("dish-1", state: "ON", price: 10m, popularity: 5);

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(existingOrder);
        await _context.SaveAsync(dish);

        var dto = new DeleteDishFromOrderDTO
        {
            OperationId = "op-del-1",
            DishId = "dish-1",
            Quantity = 1
        };

        var result = await _sut.DeleteDishAsync("waiter-1", reservation.Id, dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var reservationAfter = await _context.LoadAsync<Reservation>(reservation.Id);
        reservationAfter!.DishCount.Should().Be(1);

        var orderAfter = await _context.LoadAsync<Order>(reservation.Id);
        orderAfter!.Dishes.Should().ContainSingle();
        orderAfter.Dishes[0].Quantity.Should().Be(1);

        var dishAfter = await _context.LoadAsync<Dish>(dish.Id);
        dishAfter!.Popularity.Should().Be(5);
    }

    [Fact]
    public async Task CompleteAsync_WhenValidRequest_CompletesOrder()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.MealsServed, dishCount: 2);
        var existingOrder = BuildOrder(reservation.Id);
        var dish = BuildDish("dish-1", state: "ON", price: 10m, popularity: 5);

        await _context.SaveAsync(dish);
        await _context.SaveAsync(reservation);
        await _context.SaveAsync(existingOrder);

        var dto = new CompleteOrderDTO
        {
            OperationId = "op-complete-1"
        };

        var result = await _sut.CompleteAsync("waiter-1", reservation.Id, dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Completed);

        var orderAfter = await _context.LoadAsync<Order>(reservation.Id);
        orderAfter!.Status.Should().Be(OrderStatus.Completed);
        orderAfter.CompletedAt.Should().NotBeNull();
        var dishAfter = await _context.LoadAsync<Dish>("dish-1");
        dishAfter!.Popularity.Should().Be(7);
        dishAfter.PopularityFlag.Should().Be("true");
    }

    private static CreateOrderDTO BuildDto(string reservationId, params (string DishId, int Quantity)[] dishes)
        => new()
        {
            ReservationId = reservationId,
            Dishes = dishes
                .Select(x => new OrderDishItemDTO
                {
                    DishId = x.DishId,
                    Quantity = x.Quantity
                })
                .ToList()
        };

    private static Reservation BuildReservation(string waiterId, ReservationStatus status, int dishCount)
    {
        var id = $"res-{Guid.NewGuid():N}";
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

    private static Dish BuildDish(string? id = null, string state = "ON", decimal price = 12m, int popularity = 0)
    {
        var resolvedId = id ?? $"dish-{Guid.NewGuid():N}";

        return new Dish
        {
            Id = resolvedId,
            Name = $"Dish {resolvedId}",
            DishType = "MAIN",
            Price = price,
            State = state,
            Description = "desc",
            ImageUrl = "img",
            Popularity = popularity,
            PopularityFlag = popularity > 0 ? "true" : null
        };
    }

    private static Order BuildOrder(string reservationId)
        => new()
        {
            Id = reservationId,
            ReservationId = reservationId,
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            WaiterId = "waiter-1",
            WaiterName = "Waiter",
            CustomerId = "customer-1",
            CustomerName = "Customer",
            VisitorName = null,
            TableNumber = 3,
            GuestsCount = 2,
            Status = OrderStatus.Open,
            Dishes = new List<OrderDishSnapshot>
            {
                new()
                {
                    DishId = "dish-1",
                    Name = "Dish 1",
                    Description = "desc",
                    PhotoUrl = "img",
                    PriceAtOrder = 10m,
                    Quantity = 2
                }
            },
            TotalAmount = 20m,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            CompletedAt = null,
            Version = 1,
            ProcessedOperationIds = []
        };
}