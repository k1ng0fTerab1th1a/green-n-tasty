using FluentAssertions;
using Moq;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
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

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

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

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be("customer-1");
        result.Value.CustomerName.Should().Be("Customer User");
        result.Value.WaiterId.Should().Be("waiter-1");
        result.Value.WaiterName.Should().Be("Waiter User");
        result.Value.LocationId.Should().Be("loc-1");
        result.Value.TableNumber.Should().Be(3);
        result.Value.TableKey.Should().Be("loc-1#3");
        result.Value.GuestsCount.Should().Be(2);
        result.Value.Status.Should().Be(ReservationStatus.Reserved);
        result.Value.LocationAddress.Should().Be("Main street 1");
        result.Value.IsCreatedByWaiter.Should().BeFalse();
        result.Value.VisitorName.Should().BeNull();
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

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        List<string>? capturedSlots = null;
        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, DateOnly, List<string>, CancellationToken>((_, _, slots, _) =>
            {
                capturedSlots = slots;
            })
            .ReturnsAsync(true);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.IsSuccess.Should().BeTrue();
        capturedSlots.Should().NotBeNull();
        capturedSlots!.Should().HaveCount(9);
        capturedSlots.Should().Contain(s => s.StartsWith(date.ToString("yyyy-MM-dd") + "T23:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T00:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T01:00"));

        result.Value.StartDateTime.Should().StartWith(date.ToString("yyyy-MM-dd") + "T23:00");
        result.Value.EndDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T01:00");

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

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

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

        result.IsSuccess.Should().BeTrue();
        capturedReservation.Should().NotBeNull();
        capturedSlots.Should().NotBeNull();
        capturedSlots!.Should().HaveCount(5);
        capturedSlots.Should().OnlyContain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T01:00"));
        capturedSlots.Should().Contain(s => s.StartsWith(nextDate.ToString("yyyy-MM-dd") + "T02:00"));

        result.Value.StartDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T01:00");
        result.Value.EndDateTime.Should().StartWith(nextDate.ToString("yyyy-MM-dd") + "T02:00");

        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenLocationMissing_ShouldReturnLocationNotFoundError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.LocationNotFound);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenWaiterScheduleMissing_ShouldReturnNoWaiterAssignedError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaiterSchedule?)null);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.NoWaiterAssigned);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenSlotsUnavailable_ShouldReturnSlotUnavailableError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(12, 0), new TimeOnly(13, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation());

        _waiterScheduleRepo
            .Setup(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaiterSchedule { TableKey = "loc-1#3", Date = date.ToString("yyyy-MM-dd"), WaiterId = "waiter-1" });

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

        _repo.Setup(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.SlotUnavailable);
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _waiterScheduleRepo.Verify(r => r.GetAsync("loc-1#3", date.ToString("yyyy-MM-dd"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CreateWithSlotsAsync(It.IsAny<Reservation>(), date, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateForClientAsync_WhenTimeOutsideWorkingHours_ShouldReturnValidationError()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var dto = BuildDto(date, new TimeOnly(9, 0), new TimeOnly(10, 0));

        _locationRepo.Setup(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLocation(openTime: "10:00", closeTime: "22:00"));

        var result = await _sut.CreateForClientAsync("customer-1", dto, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("working hours").And.Contain("10:00 - 22:00");
        _locationRepo.Verify(r => r.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }
}

