using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    [Fact]
    public async Task CreateForWaiterAsync_WhenExistingCustomer_ShouldCreateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO(3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));
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

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be("customer-1");
        result.Value.WaiterId.Should().Be("waiter-1");
        result.Value.LocationAddress.Should().Be("Main street 1");
        result.Value.IsCreatedByWaiter.Should().BeTrue();
        result.Value.VisitorName.Should().BeNull();

        capturedReservation.Should().NotBeNull();
        capturedReservation!.IsCreatedByWaiter.Should().BeTrue();
        capturedReservation.LocationAddress.Should().Be("Main street 1");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenAnonymousVisitor_ShouldCreateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO(3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, null, "Anna Visitor");

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));

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

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().BeNull();
        result.Value.VisitorName.Should().Be("Anna Visitor");
        result.Value.IsCreatedByWaiter.Should().BeTrue();
        result.Value.LocationAddress.Should().Be("Main street 1");
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenBothCustomerIdAndVisitorNameProvided_ShouldReturnValidationError()
    {
        var dto = new CreateReservationForWaiterDTO(
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-1",
            "Anna Visitor");

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.CustomerOrVisitorRequired);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenNeitherCustomerIdNorVisitorNameProvided_ShouldReturnValidationError()
    {
        var dto = new CreateReservationForWaiterDTO(
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            null,
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.CustomerOrVisitorRequired);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenCustomerNotFound_ShouldReturnCustomerNotFoundError()
    {
        var dto = new CreateReservationForWaiterDTO(
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-404",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-404", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.CustomerNotFound);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenCustomerRoleIsNotCustomer_ShouldReturnCustomerNotFoundError()
    {
        var dto = new CreateReservationForWaiterDTO(
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "admin-1",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));
        _userRepo.Setup(r => r.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                UserId = "admin-1",
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                Role = "ADMIN"
            });

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.CustomerNotFound);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenWaiterScheduleMissing_ShouldReturnNoWaiterAssignedError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO(3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaiterSchedule?)null);

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.NoWaiterAssigned);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenScheduleBelongsToAnotherWaiter_ShouldReturnWaiterNotAssignedError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO(3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));
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

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.WaiterNotAssignedForCreation);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenActorIsNotWaiter_ShouldReturnForbiddenError()
    {
        var dto = new CreateReservationForWaiterDTO(
            3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            2,
            "customer-1",
            null);

        _userRepo.Setup(r => r.GetByIdAsync("customer-actor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-actor"));

        var result = await _sut.CreateForWaiterAsync("customer-actor", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.Forbidden);
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_WhenActorIsNotWaiter_ShouldReturnForbiddenError()
    {
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        var result = await _sut.SearchCustomersForWaiterAsync("customer-1", "ann", default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.Forbidden);
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_WhenQueryIsEmpty_ShouldReturnEmptyList_AndNotSearchRepository()
    {
        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));

        var result = await _sut.SearchCustomersForWaiterAsync("waiter-1", "", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _userRepo.Verify(r => r.SearchCustomersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchCustomersForWaiterAsync_ShouldReturnMaskedEmailList()
    {
        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1", "loc-1"));

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

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].CustomerId.Should().Be("customer-1");
        result.Value[0].Username.Should().Be("Anna Smith");
        result.Value[0].MaskedEmail.Should().Be("a**a@example.com");
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenWaiterMovesReservationToAnotherWaiterSlot_ShouldReturnWaiterNotAssignedError()
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

        var result = await _sut.UpdateReservationAsync("waiter-1", true, dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.WaiterNotAssignedForUpdate);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenWaiterHasNoLocation_ShouldReturnWaiterLocationNotConfigured()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = new CreateReservationForWaiterDTO(3, date, new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.WaiterLocationNotConfigured);
    }
}
