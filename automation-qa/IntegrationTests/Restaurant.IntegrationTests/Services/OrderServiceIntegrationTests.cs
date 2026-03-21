using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;
using System.Text.Json;

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
        var dishRepo = new DishRepository(fixture.Context);

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

        var orderSearch = _context.QueryAsync<Order>(reservation.Id, new DynamoDBOperationConfig
        {
            IndexName = "reservationId-index"
        });
        var orders = await orderSearch.GetRemainingAsync();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsyncForReservation_WhenValidRequest_PersistsOrderAndUpdatesDishCount()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);
        var dish1 = BuildDish(state: "ON", price: 10f);
        var dish2 = BuildDish(state: "ON", price: 25f);

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(dish1);
        await _context.SaveAsync(dish2);

        var dto = BuildDto(
            reservation.Id,
            (dish1.Id, 2),
            (dish2.Id, 1));

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReservationId.Should().Be(reservation.Id);
        result.Value.WaiterId.Should().Be("waiter-1");
        result.Value.TotalAmount.Should().Be(45f);

        var reservationAfter = await _context.LoadAsync<Reservation>(reservation.Id);
        reservationAfter.Should().NotBeNull();
        reservationAfter!.DishCount.Should().Be(3);

        var orderSearch = _context.QueryAsync<Order>(reservation.Id, new DynamoDBOperationConfig
        {
            IndexName = "reservationId-index"
        });
        var orders = await orderSearch.GetRemainingAsync();

        orders.Should().HaveCount(1);
        orders[0].TotalAmount.Should().Be(45f);

        var snapshots = JsonSerializer.Deserialize<List<OrderDishSnapshot>>(orders[0].DishesJson);
        snapshots.Should().NotBeNull();
        snapshots!.Sum(x => x.Quantity).Should().Be(3);
    }

    private static CreateOrderDTO BuildDto(string reservationId, params (string dishId, int quantity)[] items)
        => new()
        {
            ReservationId = reservationId,
            Dishes = items.Select(x => new OrderDishItemDTO
            {
                DishId = x.dishId,
                Quantity = x.quantity
            }).ToList()
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

    private static Dish BuildDish(string state, float price = 12f)
        => new()
        {
            Id = $"dish-{Guid.NewGuid():N}",
            Name = "Dish",
            DishType = "MAIN",
            Price = price,
            State = state,
            Description = "desc",
            ImageUrl = "img"
        };
}