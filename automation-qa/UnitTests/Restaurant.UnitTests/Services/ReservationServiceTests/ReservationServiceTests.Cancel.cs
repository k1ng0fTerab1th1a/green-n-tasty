using FluentAssertions;
using Moq;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    [Fact]
    public async Task CancelReservation_WhenReservationNotFound_ShouldThrowArgumentNullException()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var act = async () => await _sut.CancelReservation("missing", "customer-1", isWaiter: false, ct: default);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("Reservation does not exist*");

        _repo.Verify(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
        _waiterScheduleRepo.VerifyNoOtherCalls();
        _tableRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelReservation_WhenStatusIsNotReserved_ShouldThrowBusinessException()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Cancelled,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var act = async () => await _sut.CancelReservation("r1", "customer-1", isWaiter: false, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Only reserved reservations can be cancelled.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelReservation_WhenLessThan30MinutesBeforeStart_ShouldThrowBusinessException()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(20).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddMinutes(80).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var act = async () => await _sut.CancelReservation("r1", "customer-1", isWaiter: false, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Reservation cannot be cancelled less than 30 minutes before it starts.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelReservation_WhenRepositoryReturnsFalse_ShouldThrowBusinessException()
    {
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = start.AddHours(1);
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = start.ToString("O"),
            EndDateTime = end.ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _repo.Setup(r => r.CancelReservationAsync(
                reservation,
                It.Is<List<string>>(slots => slots.Count == 5 && slots[0] == start.ToString("yyyy-MM-ddTHH:mmzzz")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.CancelReservation("r1", "customer-1", isWaiter: false, ct: default);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Failed to cancel reservation.");

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CancelReservationAsync(reservation, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelReservation_WhenValid_ShouldReturnTrue_AndCallRepositoryWithGeneratedSlots()
    {
        var start = DateTimeOffset.UtcNow.AddHours(3);
        var end = start.AddHours(1);
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = start.ToString("O"),
            EndDateTime = end.ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _repo.Setup(r => r.CancelReservationAsync(
                reservation,
                It.Is<List<string>>(slots => slots.Count == 5 && slots[0] == start.ToString("yyyy-MM-ddTHH:mmzzz")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CancelReservation("r1", "customer-1", isWaiter: false, ct: default);

        result.Should().BeTrue();
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.CancelReservationAsync(reservation, It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }
}
