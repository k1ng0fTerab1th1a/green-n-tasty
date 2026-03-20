using FluentAssertions;
using Moq;
using Restaurant.Core.Exceptions;
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

        result.CustomerId.Should().Be("customer-1");
        result.CustomerName.Should().Be("Customer User");
        result.WaiterId.Should().Be("waiter-1");
        result.WaiterName.Should().Be("Waiter User");
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

        _userRepo.Setup(r => r.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildWaiter("waiter-1"));
        _userRepo.Setup(r => r.GetByIdAsync("customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCustomer("customer-1"));

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
}
