using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _repo;
    private readonly Mock<IWaiterScheduleRepository> _waiterScheduleRepo;
    private readonly Mock<ILocationRepository> _locationRepo;
    private readonly Mock<ITableRepository> _tableRepo;
    private readonly ReservationService _sut;

    public ReservationServiceTests()
    {
        _repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _waiterScheduleRepo = new Mock<IWaiterScheduleRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        _tableRepo = new Mock<ITableRepository>(MockBehavior.Strict);

        _sut = new ReservationService(_repo.Object, _waiterScheduleRepo.Object, _locationRepo.Object, _tableRepo.Object);
    }

    [Fact]
    public async Task GetMyAsync_WhenActorUserIdIsEmpty_ShouldReturnEmpty_AndNotCallRepo()
    {
        var result = await _sut.GetMyAsync("", actorIsWaiter: false, ct: default);

        result.Should().BeEmpty();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyAsync_WhenActorIsWaiter_ShouldQueryByWaiter()
    {
        _repo.Setup(r => r.QueryByWaiterAsync("w1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c", WaiterId = "w1", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var result = await _sut.GetMyAsync("w1", actorIsWaiter: true, ct: default);

        result.Should().HaveCount(1);
        _repo.Verify(r => r.QueryByWaiterAsync("w1", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyAsync_WhenActorIsCustomer_ShouldQueryByCustomer()
    {
        _repo.Setup(r => r.QueryByCustomerAsync("c1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c1", WaiterId = "w", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var result = await _sut.GetMyAsync("c1", actorIsWaiter: false, ct: default);

        result.Should().HaveCount(1);
        _repo.Verify(r => r.QueryByCustomerAsync("c1", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.GetByIdAsync("r1", actorUserId: "c1", actorIsWaiter: false, ct: default);

        result.Should().BeNull();
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerOwner_ShouldReturnEntity()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = "r1",
                CustomerId = "customerA",
                WaiterId = "waiterA",
                LocationId = "l",
                TableNumber = 1,
                TableKey = "l#1",
                StartDateTime = "2026-03-05T10:00:00Z",
                EndDateTime = "2026-03-05T11:30:00Z",
                GuestsCount = 2,
                CreatedAt = "c",
                UpdatedAt = "u"
            });

        var result = await _sut.GetByIdAsync("r1", actorUserId: "customerA", actorIsWaiter: false, ct: default);

        result.Should().NotBeNull();
        result!.Id.Should().Be("r1");
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenWaiterAssigned_ShouldReturnEntity()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = "r1",
                CustomerId = "customerA",
                WaiterId = "waiterA",
                LocationId = "l",
                TableNumber = 1,
                TableKey = "l#1",
                StartDateTime = "2026-03-05T10:00:00Z",
                EndDateTime = "2026-03-05T11:30:00Z",
                GuestsCount = 2,
                CreatedAt = "c",
                UpdatedAt = "u"
            });

        var result = await _sut.GetByIdAsync("r1", actorUserId: "waiterA", actorIsWaiter: true, ct: default);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotAllowed_ShouldThrow()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = "r1",
                CustomerId = "customerA",
                WaiterId = "waiterA",
                LocationId = "l",
                TableNumber = 1,
                TableKey = "l#1",
                StartDateTime = "2026-03-05T10:00:00Z",
                EndDateTime = "2026-03-05T11:30:00Z",
                GuestsCount = 2,
                CreatedAt = "c",
                UpdatedAt = "u"
            });

        var act = async () => await _sut.GetByIdAsync("r1", actorUserId: "someoneElse", actorIsWaiter: false, ct: default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenActorIsDifferentWaiter_ShouldThrow()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = "r1",
                CustomerId = "customerA",
                WaiterId = "waiterA",
                LocationId = "l",
                TableNumber = 1,
                TableKey = "l#1",
                StartDateTime = "2026-03-05T10:00:00Z",
                EndDateTime = "2026-03-05T11:30:00Z",
                GuestsCount = 2,
                CreatedAt = "c",
                UpdatedAt = "u"
            });

        var act = async () => await _sut.GetByIdAsync("r1", actorUserId: "waiterB", actorIsWaiter: true, ct: default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Forbidden.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenValidInput_ShouldCreateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

        Reservation? capturedReservation = null;
        List<string>? capturedSlots = null;
        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, DateOnly, List<string>, CancellationToken>((reservation, _, slots, _) =>
            {
                capturedReservation = reservation;
                capturedSlots = slots;
            })
            .ReturnsAsync(true);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.CustomerId.Should().Be("customer-1");
        result.WaiterId.Should().Be("waiter-1");
        result.LocationId.Should().Be("loc-1");
        result.TableNumber.Should().Be(3);
        result.TableKey.Should().Be("loc-1#3");
        result.GuestsCount.Should().Be(2);
        result.Status.Should().Be(ReservationStatus.Reserved);
        capturedReservation.Should().NotBeNull();
        capturedSlots.Should().NotBeNull();
        capturedSlots!.Should().HaveCount(5);

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenLocationMissing_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var act = async () => await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Location not found.");

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenWaiterScheduleMissing_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaiterSchedule?)null);

        var act = async () => await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("No waiter assigned for this table on this date.");

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenSlotsUnavailable_ShouldThrowSlotUnavailableException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        await act.Should().ThrowAsync<SlotUnavailableException>();

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenTimeOutsideWorkingHours_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(9, 0), new TimeOnly(10, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "10:00", closeTime: "22:00"));

        var act = async () => await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Reservation must be within working hours*10:00 - 22:00*");

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    private static CreateReservationDTO BuildDto(DateOnly date, TimeOnly from, TimeOnly to)
        => new("loc-1", 3, date, from, to, 2);

    private static Location BuildLocation(string openTime = "10:00", string closeTime = "22:00")
        => new()
        {
            Id = "loc-1",
            Address = "Main street 1",
            Description = "Test location",
            TimeZone = "UTC",
            OpenTime = openTime,
            CloseTime = closeTime,
            ImageUrl = "http://img/loc-1",
            TotalCapacity = 120,
            AverageOccupancy = 0.35,
            Rating = 4.6
        };
}
