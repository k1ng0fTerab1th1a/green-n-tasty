using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    [Fact]
    public async Task CreateForWaiterAsync_WhenExistingCustomer_ShouldCreateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule
            {
                TableKey = "loc-1#3",
                Date = date.ToString("yyyy-MM-dd"),
                WaiterId = "waiter-1"
            });

        Reservation? capturedReservation = null;
        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, DateOnly, List<string>, CancellationToken>((reservation, _, _, _) => capturedReservation = reservation)
            .ReturnsAsync(true);

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.CustomerId.Should().Be("customer-1");
        result.WaiterId.Should().Be("waiter-1");
        result.LocationAddress.Should().Be("Main street 1");
        result.IsCreatedByWaiter.Should().BeTrue();
        result.VisitorName.Should().BeNull();

        capturedReservation.Should().NotBeNull();
        capturedReservation!.IsCreatedByWaiter.Should().BeTrue();
        capturedReservation.LocationAddress.Should().Be("Main street 1");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenAnonymousVisitor_ShouldCreateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, null, "Anna Visitor");

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule
            {
                TableKey = "loc-1#3",
                Date = date.ToString("yyyy-MM-dd"),
                WaiterId = "waiter-1"
            });

        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.CustomerId.Should().BeNull();
        result.VisitorName.Should().Be("Anna Visitor");
        result.IsCreatedByWaiter.Should().BeTrue();
        result.LocationAddress.Should().Be("Main street 1");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenBothCustomerIdAndVisitorNameProvided_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO(
            "loc-1",
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-1",
            "Anna Visitor");

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Exactly one of customerId or visitorName must be provided.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenNeitherCustomerIdNorVisitorNameProvided_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO(
            "loc-1",
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            null,
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Exactly one of customerId or visitorName must be provided.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenCustomerNotFound_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO(
            "loc-1",
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-404",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-404", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Customer not found.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenCustomerRoleIsNotCustomer_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO(
            "loc-1",
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "admin-1",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                UserId = "admin-1",
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                Role = "ADMIN"
            });

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Customer not found.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenWaiterScheduleMissing_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaiterSchedule?)null);

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("No waiter assigned for this table on this date.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenScheduleBelongsToAnotherWaiter_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo.Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule
            {
                TableKey = "loc-1#3",
                Date = date.ToString("yyyy-MM-dd"),
                WaiterId = "waiter-2"
            });

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Waiter can create reservations only for assigned tables.");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenActorIsNotWaiter_ShouldThrowUnauthorizedAccessException()
    {
        var dto = new CreateReservationForWaiterDTO(
            "loc-1",
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-1",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("customer-actor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-actor"));

        var act = async () => await _sut.CreateForWaiterAsync("customer-actor", dto, default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Forbidden.");
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_WhenActorIsNotWaiter_ShouldThrowUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        var act = async () => await _sut.SearchCustomersForWaiterAsync("customer-1", "ann", default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Forbidden.");
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_WhenQueryIsEmpty_ShouldReturnEmptyList_AndNotSearchRepository()
    {
        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var result = await _sut.SearchCustomersForWaiterAsync("waiter-1", "", default);

        result.Should().BeEmpty();
        _userRepo.Verify(r => r.SearchCustomersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_ShouldReturnMaskedEmailList()
    {
        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        _userRepo.Setup(r => r.SearchCustomersAsync("ann", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new()
                {
                    UserId = "customer-1",
                    FirstName = "Anna",
                    LastName = "Smith",
                    Email = "anna@example.com",
                    Role = "CUSTOMER"
                }
            });

        var result = await _sut.SearchCustomersForWaiterAsync("waiter-1", "ann", default);

        result.Should().HaveCount(1);
        result[0].CustomerId.Should().Be("customer-1");
        result[0].Username.Should().Be("Anna Smith");
        result[0].MaskedEmail.Should().Be("a**a@example.com");
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenWaiterMovesReservationToAnotherWaiterSlot_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var dto = new UpdateReservationDTO("r1", 3, 4, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero).ToString("O"),
            EndDateTime = new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _tableRepo.Setup(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Table
            {
                LocationId = "loc-1",
                TableNumber = 4,
                LocationAddress = "Main street 1",
                Capacity = 6
            });

        _waiterScheduleRepo.Setup(r => r.GetAsync("loc-1#4", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule
            {
                TableKey = "loc-1#4",
                Date = date.ToString("yyyy-MM-dd"),
                WaiterId = "waiter-2"
            });

        var act = async () => await _sut.UpdateReservationAsync("waiter-1", true, dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Waiter can update reservations only for assigned tables.");
    }
}