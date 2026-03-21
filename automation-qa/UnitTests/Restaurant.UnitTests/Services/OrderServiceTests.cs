using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using System.Text.Json;

namespace Restaurant.UnitTests.Services;

public sealed class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<IReservationRepository> _reservationRepo;
    private readonly Mock<IDishRepository> _dishRepo;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        _reservationRepo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _dishRepo = new Mock<IDishRepository>(MockBehavior.Strict);

        _sut = new OrderService(_orderRepo.Object, _reservationRepo.Object, _dishRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenReservationNotFound_ReturnsNotFoundError()
    {
        var dto = BuildDto("res-1", ("dish-1", 1));

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.ReservationNotFound);

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenWaiterNotAssigned_ReturnsForbidden()
    {
        var dto = BuildDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-2", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.Forbidden);

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenOrderAlreadyExists_ReturnsConflict()
    {
        var dto = BuildDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 2);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.OrderAlreadyExists);

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenReservationStatusNotInProgress_ReturnsError()
    {
        var dto = BuildDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.Reserved, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.ReservationStatusNotOrderable);

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenDishNotFound_ReturnsDishNotFoundError()
    {
        var dto = BuildDto("res-1", ("dish-1", 1), ("dish-2", 2));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { BuildDish("dish-1", "ON", 12f) });

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.DishNotFound("dish-2"));

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenDishNotAvailable_ReturnsDishNotAvailableError()
    {
        var dto = BuildDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { BuildDish("dish-1", "OFF", 12f) });

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.DishNotAvailable("dish-1"));

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesOrderAndUpdatesDishCount()
    {
        var dto = BuildDto("res-1", ("dish-1", 2), ("dish-2", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>
            {
                BuildDish("dish-1", "ON", 10f),
                BuildDish("dish-2", "ON", 25f)
            });

        Order? capturedOrder = null;
        int capturedDishCount = -1;

        _orderRepo
            .Setup(r => r.CreateWithReservationUpdateAsync(
                It.IsAny<Order>(),
                "res-1",
                "waiter-1",
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Order, string, string, int, string, CancellationToken>((order, _, _, dishCount, _, _) =>
            {
                capturedOrder = order;
                capturedDishCount = dishCount;
            })
            .ReturnsAsync(true);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReservationId.Should().Be("res-1");
        result.Value.WaiterId.Should().Be("waiter-1");
        result.Value.Status.Should().Be(OrderStatus.Open);
        result.Value.TotalAmount.Should().Be(45f);
        capturedDishCount.Should().Be(3);

        capturedOrder.Should().NotBeNull();
        var snapshots = JsonSerializer.Deserialize<List<OrderDishSnapshot>>(capturedOrder!.DishesJson);
        snapshots.Should().NotBeNull();
        snapshots!.Should().HaveCount(2);
        snapshots.Sum(x => x.Quantity).Should().Be(3);

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRepo.Verify(r => r.CreateWithReservationUpdateAsync(
            It.IsAny<Order>(),
            "res-1",
            "waiter-1",
            3,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    private static CreateOrderDTO BuildDto(string reservationId, params (string dishId, int qty)[] items)
        => new()
        {
            ReservationId = reservationId,
            Dishes = items
                .Select(x => new OrderDishItemDTO { DishId = x.dishId, Quantity = x.qty })
                .ToList()
        };

    private static Reservation BuildReservation(string waiterId, ReservationStatus status, int dishCount)
        => new()
        {
            Id = "res-1",
            CustomerId = "customer-1",
            CustomerName = "Customer One",
            WaiterId = waiterId,
            WaiterName = "Waiter One",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"),
            GuestsCount = 2,
            DishCount = dishCount,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O")
        };

    private static Dish BuildDish(string id, string state, float price)
        => new()
        {
            Id = id,
            Name = $"Dish {id}",
            DishType = "MAIN",
            Price = price,
            State = state,
            Description = "desc",
            ImageUrl = "img"
        };
}