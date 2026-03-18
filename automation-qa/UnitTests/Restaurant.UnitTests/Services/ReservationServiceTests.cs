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
    private readonly Mock<IUserRepository> _userRepo;
    private readonly ReservationService _sut;

    public ReservationServiceTests()
    {
        _repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _waiterScheduleRepo = new Mock<IWaiterScheduleRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        _tableRepo = new Mock<ITableRepository>(MockBehavior.Strict);
        _userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        _sut = new ReservationService(
            _repo.Object,
            _waiterScheduleRepo.Object,
            _locationRepo.Object,
            _tableRepo.Object,
            _userRepo.Object);
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
        result.LocationAddress.Should().Be("Main street 1");
        result.IsCreatedByWaiter.Should().BeFalse();
        result.VisitorName.Should().BeNull();
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
    public async Task CreateForClientAsync_WhenCrossingMidnight_ShouldSendSlotsAcrossTwoCalendarDates()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var nextDate = date.AddDays(1);
        var dto = BuildDto(date, new TimeOnly(23, 0), new TimeOnly(1, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "18:00", closeTime: "04:00"));

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

        List<string>? capturedSlots = null;
        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, DateOnly, List<string>, CancellationToken>((_, _, slots, _) =>
            {
                capturedSlots = slots;
            })
            .ReturnsAsync(true);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        capturedSlots.Should().NotBeNull();
        capturedSlots!.Should().HaveCount(9);
        capturedSlots.Should().Contain(s => s.StartsWith(date.ToString("yyyy-MM-dd") + "T23:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T00:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T01:00"));

        result.StartDateTime.Should().StartWith(date.ToString("yyyy-MM-dd") + "T23:00");
        result.EndDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T01:00");

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenBothTimesAfterMidnight_ShouldShiftSlotsToNextCalendarDate()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var nextDate = date.AddDays(1);
        var dto = BuildDto(date, new TimeOnly(1, 0), new TimeOnly(2, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "18:00", closeTime: "04:00"));

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

        capturedReservation.Should().NotBeNull();
        capturedSlots.Should().NotBeNull();
        capturedSlots!.Should().HaveCount(5);
        capturedSlots.Should().OnlyContain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T01:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T02:00"));

        result.StartDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T01:00");
        result.EndDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T02:00");

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

    [Fact]
    public async Task UpdateReservationAsync_WhenValidInput_ShouldUpdateReservation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var dto = new UpdateReservationDTO("r1", 3, 4, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
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

        _repo.Setup(r => r.UpdateReservationAsync(
                It.IsAny<Reservation>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                "loc-1#3",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.UpdateReservationAsync("customer-1", false, dto);

        result.Should().NotBeNull();
        result!.TableNumber.Should().Be(4);
        result.GuestsCount.Should().Be(3);

        _repo.Verify(r => r.UpdateReservationAsync(
            It.IsAny<Reservation>(),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            "loc-1#3",
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.Verify(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenCrossingMidnight_ShouldSendSlotsAcrossTwoCalendarDates()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var nextDate = date.AddDays(1);
        var dto = new UpdateReservationDTO("r1", 3, 4, date, new TimeOnly(23, 0), new TimeOnly(1, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(4).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "18:00", closeTime: "04:00"));

        _tableRepo.Setup(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Table
            {
                LocationId = "loc-1",
                TableNumber = 4,
                LocationAddress = "Main street 1",
                Capacity = 6
            });

        List<string>? capturedNewSlots = null;
        _repo.Setup(r => r.UpdateReservationAsync(
                It.IsAny<Reservation>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                "loc-1#3",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<Reservation, List<string>, List<string>, string, DateTimeOffset, CancellationToken>((_, newSlots, _, _, _, _) =>
            {
                capturedNewSlots = newSlots;
            })
            .ReturnsAsync(existing);

        await _sut.UpdateReservationAsync("customer-1", false, dto);

        capturedNewSlots.Should().NotBeNull();
        capturedNewSlots!.Should().Equal(
            $"{date:yyyy-MM-dd}T23:00+00:00",
            $"{date:yyyy-MM-dd}T23:15+00:00",
            $"{date:yyyy-MM-dd}T23:30+00:00",
            $"{date:yyyy-MM-dd}T23:45+00:00",
            $"{nextDate:yyyy-MM-dd}T00:00+00:00",
            $"{nextDate:yyyy-MM-dd}T00:15+00:00",
            $"{nextDate:yyyy-MM-dd}T00:30+00:00",
            $"{nextDate:yyyy-MM-dd}T00:45+00:00",
            $"{nextDate:yyyy-MM-dd}T01:00+00:00");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.Verify(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateReservationAsync(
            It.IsAny<Reservation>(),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            "loc-1#3",
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenBothTimesAfterMidnight_ShouldShiftSlotsToNextCalendarDate()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var nextDate = date.AddDays(1);
        var dto = new UpdateReservationDTO("r1", 3, 4, date, new TimeOnly(1, 0), new TimeOnly(2, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(4).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "18:00", closeTime: "04:00"));

        _tableRepo.Setup(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Table
            {
                LocationId = "loc-1",
                TableNumber = 4,
                LocationAddress = "Main street 1",
                Capacity = 6
            });

        List<string>? capturedNewSlots = null;
        _repo.Setup(r => r.UpdateReservationAsync(
                It.IsAny<Reservation>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                "loc-1#3",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<Reservation, List<string>, List<string>, string, DateTimeOffset, CancellationToken>((_, newSlots, _, _, _, _) =>
            {
                capturedNewSlots = newSlots;
            })
            .ReturnsAsync(existing);

        await _sut.UpdateReservationAsync("customer-1", false, dto);

        capturedNewSlots.Should().NotBeNull();
        capturedNewSlots!.Should().Equal(
            $"{nextDate:yyyy-MM-dd}T01:00+00:00",
            $"{nextDate:yyyy-MM-dd}T01:15+00:00",
            $"{nextDate:yyyy-MM-dd}T01:30+00:00",
            $"{nextDate:yyyy-MM-dd}T01:45+00:00",
            $"{nextDate:yyyy-MM-dd}T02:00+00:00");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.Verify(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateReservationAsync(
            It.IsAny<Reservation>(),
            It.IsAny<List<string>>(),
            It.IsAny<List<string>>(),
            "loc-1#3",
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenStatusNotReserved_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var dto = new UpdateReservationDTO("r1", 2, 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Cancelled,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = async () => await _sut.UpdateReservationAsync("customer-1", false, dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Only reserved reservations can be updated.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenLessThan30MinutesBeforeStart_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var dto = new UpdateReservationDTO("r1", 2, 3, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(20).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        var act = async () => await _sut.UpdateReservationAsync("customer-1", false, dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Reservation cannot be updated less than 30 minutes before it starts.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenTableNotFound_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var dto = new UpdateReservationDTO("r1", 2, 5, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            CreatedAt = "c",
            UpdatedAt = "u"
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _tableRepo.Setup(r => r.GetByLocationAndTableNumberAsync("loc-1", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Table?)null);

        var act = async () => await _sut.UpdateReservationAsync("customer-1", false, dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Table not found.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.Verify(r => r.GetByLocationAndTableNumberAsync("loc-1", 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_WhenGuestsExceedCapacity_ShouldThrowBusinessException()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var dto = new UpdateReservationDTO("r1", 10, 4, date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        var existing = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            StartDateTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
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
                Capacity = 4
            });

        var act = async () => await _sut.UpdateReservationAsync("customer-1", false, dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Amount of guests exceeds the table capacity.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.Verify(r => r.GetByLocationAndTableNumberAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
    }


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
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

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

        _userRepo.Verify(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.Verify(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()), Times.Once);
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
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        result.CustomerId.Should().BeNull();
        result.VisitorName.Should().Be("Anna Visitor");
        result.IsCreatedByWaiter.Should().BeTrue();
        result.LocationAddress.Should().Be("Main street 1");

        _userRepo.Verify(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenBothCustomerIdAndVisitorNameProvided_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-1", "Anna Visitor");

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Exactly one of customerId or visitorName must be provided.");

        _userRepo.Verify(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenNeitherCustomerIdNorVisitorNameProvided_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), new TimeOnly(12, 0), new TimeOnly(13, 0), 2, null, null);

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));

        var act = async () => await _sut.CreateForWaiterAsync("waiter-1", dto, default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Exactly one of customerId or visitorName must be provided.");

        _userRepo.Verify(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateForWaiterAsync_WhenCustomerNotFound_ShouldThrowBusinessException()
    {
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "customer-404", null);

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
        var dto = new CreateReservationForWaiterDTO("loc-1", 3, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), new TimeOnly(12, 0), new TimeOnly(13, 0), 2, "admin-1", null);

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

        _waiterScheduleRepo.Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
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

    private static User BuildWaiter(string userId)
        => new()
        {
            UserId = userId,
            FirstName = "Waiter",
            LastName = "User",
            Email = "waiter@example.com",
            Role = "WAITER"
        };

    private static User BuildCustomer(string userId)
        => new()
        {
            UserId = userId,
            FirstName = "Customer",
            LastName = "User",
            Email = "customer@example.com",
            Role = "CUSTOMER"
        };
}
