using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

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
    public async Task CreateAsync_WhenWaiterNotAssigned_ReturnsForbidden()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 1));
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
    public async Task CreateAsync_WhenReservationStatusNotInProgress_ReturnsError()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 1));
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
        var dto = BuildCreateDto("res-1", ("dish-1", 1), ("dish-2", 2));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { BuildDish("dish-1", "ON", 12m) });

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.DishNotFound("dish-2"));

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();

        _orderRepo.Verify(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _orderRepo.VerifyNoOtherCalls();

        _dishRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenDishNotAvailable_ReturnsDishNotAvailableError()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish> { BuildDish("dish-1", "OFF", 12m) });

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.DishNotAvailable("dish-1"));

        _reservationRepo.Verify(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();

        _orderRepo.Verify(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()), Times.Once);
        _orderRepo.VerifyNoOtherCalls();

        _dishRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenReservationNotFound_ReturnsNotFoundError()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 1));

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.ReservationNotFound);

        _reservationRepo.VerifyAll();
        _dishRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenOrderAlreadyExists_ReturnsConflict()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildOrder("res-1"));

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.OrderAlreadyExists);

        _reservationRepo.VerifyAll();
        _orderRepo.VerifyAll();
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesOrderAndUpdatesDishCount()
    {
        var dto = BuildCreateDto("res-1", ("dish-1", 2), ("dish-2", 1));
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress, dishCount: 0);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _dishRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dish>
            {
                BuildDish("dish-1", "ON", 10m),
                BuildDish("dish-2", "ON", 25m)
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

        var result = await _sut.CreateAsyncForReservation("waiter-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("res-1");
        result.Value.ReservationId.Should().Be("res-1");
        result.Value.WaiterId.Should().Be("waiter-1");
        result.Value.Status.Should().Be(OrderStatus.Open);
        result.Value.TotalAmount.Should().Be(45m);

        capturedDishCount.Should().Be(3);
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Id.Should().Be("res-1");
        capturedOrder.ReservationId.Should().Be("res-1");
        capturedOrder.Dishes.Should().HaveCount(2);

        _reservationRepo.VerifyAll();
        _orderRepo.VerifyAll();
        _dishRepo.VerifyAll();
    }

    [Fact]
    public async Task GetByReservationAsync_WhenOrderExists_ReturnsOrder()
    {
        var order = BuildOrder("res-1");

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetByReservationAsync("waiter-1", "res-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("res-1");

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddDishAsync_WhenOperationAlreadyProcessed_ReturnsCurrentOrderWithoutMutation()
    {
        var order = BuildOrder("res-1");
        order.ProcessedOperationIds.Add("op-1");

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new AddDishToOrderDTO
        {
            OperationId = "op-1",
            DishId = "dish-1",
            Quantity = 1
        };

        var result = await _sut.AddDishAsync("waiter-1", "res-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessedOperationIds.Should().Contain("op-1");

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddDishAsync_WhenValidRequest_UpdatesOrderAndReservation()
    {
        var order = BuildOrder("res-1");
        var dto = new AddDishToOrderDTO
        {
            OperationId = "op-2",
            DishId = "dish-2",
            Quantity = 2
        };

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _dishRepo
            .Setup(r => r.GetDishByIdAsync("dish-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildDish("dish-2", "ON", 25m));

        _orderRepo
            .Setup(r => r.UpdateWithReservationDishCountAsync(
                It.IsAny<Order>(),
                1,
                "op-2",
                2,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.AddDishAsync("waiter-1", "res-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dishes.Should().ContainSingle(x => x.DishId == "dish-2" && x.Quantity == 2);
        result.Value.TotalAmount.Should().Be(70m);
        result.Value.Version.Should().Be(2);
        result.Value.ProcessedOperationIds.Should().Contain("op-2");

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyAll();
    }

    [Fact]
    public async Task DeleteDishAsync_WhenQuantityTooLarge_ReturnsValidationError()
    {
        var order = BuildOrder("res-1");

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new DeleteDishFromOrderDTO
        {
            OperationId = "op-3",
            DishId = "dish-1",
            Quantity = 5
        };

        var result = await _sut.DeleteDishAsync("waiter-1", "res-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(OrderErrors.InvalidDishRemovalQuantity("dish-1").Message);

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAsync_WhenValidRequest_CompletesOrder()
    {
        var order = BuildOrder("res-1");

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepo
            .Setup(r => r.CompleteAsync(
                It.IsAny<Order>(),
                "waiter-1",
                1,
                "op-4",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _dishRepo
            .Setup(r => r.IncrementPopularityAsync("dish-1", 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CompleteOrderDTO
        {
            OperationId = "op-4"
        };

        var result = await _sut.CompleteAsync("waiter-1", "res-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Completed);
        result.Value.CompletedAt.Should().NotBeNull();
        result.Value.Version.Should().Be(2);
        result.Value.ProcessedOperationIds.Should().Contain("op-4");

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.Verify(r => r.IncrementPopularityAsync("dish-1", 2, It.IsAny<CancellationToken>()), Times.Once);
        _dishRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByReservationAsync_WhenOrderBelongsToAnotherWaiter_ReturnsForbidden()
    {
        var order = BuildOrder("res-1");
        order.WaiterId = "waiter-2";

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetByReservationAsync("waiter-1", "res-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(OrderErrors.Forbidden);

        _orderRepo.VerifyAll();
        _reservationRepo.VerifyNoOtherCalls();
        _dishRepo.VerifyNoOtherCalls();
    }

    private static CreateOrderDTO BuildCreateDto(string reservationId, params (string dishId, int qty)[] items)
        => new()
        {
            ReservationId = reservationId,
            Dishes = items
                .Select(x => new OrderDishItemDTO
                {
                    DishId = x.dishId,
                    Quantity = x.qty
                })
                .ToList()
        };

    private static Reservation BuildReservation(
        string waiterId,
        ReservationStatus status,
        int dishCount,
        bool isMealServed = false)
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
        IsMealServed = isMealServed,
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1).ToString("O"),
        UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O")
    };

    private static Dish BuildDish(string id, string state, decimal price)
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

    private static Order BuildOrder(string reservationId)
        => new()
        {
            Id = reservationId,
            ReservationId = reservationId,
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            WaiterId = "waiter-1",
            WaiterName = "Waiter One",
            CustomerId = "customer-1",
            CustomerName = "Customer One",
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
