using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
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
}
