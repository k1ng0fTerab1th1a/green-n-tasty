using FluentAssertions;
using Moq;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    private static List<string> GenerateExpectedSlots(DateTimeOffset start, DateTimeOffset end)
    {
        var slots = new List<string>();
        var cursor = start;

        while (true)
        {
            slots.Add(cursor.ToString("yyyy-MM-ddTHH:mmzzz"));
            if (cursor >= end) break;
            cursor = cursor.AddMinutes(15);
        }

        return slots;
    }

    [Fact]
    public async Task StartReservationAsync_WhenReservationNotFound_ShouldReturnFailedResult()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.StartReservationAsync("missing", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        _repo.Verify(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartReservationAsync_WhenStatusIsNotReserved_ShouldReturnNotStartableError()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.StartReservationAsync("r1", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.NotStartable.Message);

        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartReservationAsync_WhenValid_ShouldSetInProgress_AndActualStartTime()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _repo.Setup(r => r.UpdateLifecycleAsync(
                reservation,
                ReservationStatus.Reserved,
                ReservationStatus.InProgress,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.StartReservationAsync("r1", "waiter-1", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.InProgress);
        result.Value.ActualStartTime.Should().NotBeNullOrWhiteSpace();
        result.Value.ActualEndTime.Should().BeNull();

        _repo.Verify(r => r.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.Reserved,
            ReservationStatus.InProgress,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkMealsServedAsync_WhenReservationNotFound_ShouldReturnFailedResult()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.MarkMealsServedAsync("missing", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        _repo.Verify(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MarkMealsServedAsync_WhenStatusIsNotInProgress_ShouldReturnNotMarkableError()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-20).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.MarkMealsServedAsync("r1", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.NotMarkable.Message);
    }

    [Fact]
    public async Task MarkMealsServedAsync_WhenValid_ShouldSetMealsServedStatus()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-20).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _repo.Setup(r => r.UpdateLifecycleAsync(
                reservation,
                ReservationStatus.InProgress,
                ReservationStatus.MealsServed,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.MarkMealsServedAsync("r1", "waiter-1", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.MealsServed);
        result.Value.ActualStartTime.Should().NotBeNullOrWhiteSpace();
        result.Value.ActualEndTime.Should().BeNull();
    }

    [Fact]
    public async Task FinishReservationAsync_WhenReservationNotFound_ShouldReturnFailedResult()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        _orderRepo.Setup(r => r.CompleteOnReservationFinishIfOpenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.FinishReservationAsync("missing", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        _repo.Verify(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FinishReservationAsync_WhenStatusIsNotMealsServed_ShouldReturnNotFinishableError()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddHours(-1).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-50).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo.Setup(r => r.CompleteOnReservationFinishIfOpenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.FinishReservationAsync("r1", "waiter-1", default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.NotFinishable.Message);
    }

    [Fact]
    public async Task FinishReservationAsync_WhenValid_ShouldSetFinished_ActualEndTime_AndReleaseSlots()
    {
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var end = start.AddHours(1);
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = start.ToString("O"),
            EndDateTime = end.ToString("O"),
            ActualStartTime = start.AddMinutes(5).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.MealsServed,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var expectedSlots = GenerateExpectedSlots(start, end);

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _repo.Setup(r => r.UpdateLifecycleAsync(
                reservation,
                ReservationStatus.MealsServed,
                ReservationStatus.Finished,
                It.Is<List<string>>(slots => slots.SequenceEqual(expectedSlots)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepo.Setup(r => r.CompleteOnReservationFinishIfOpenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.FinishReservationAsync("r1", "waiter-1", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.Finished);
        result.Value.ActualStartTime.Should().NotBeNullOrWhiteSpace();
        result.Value.ActualEndTime.Should().NotBeNullOrWhiteSpace();

        _repo.Verify(r => r.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.MealsServed,
            ReservationStatus.Finished,
            It.Is<List<string>>(slots => slots.SequenceEqual(expectedSlots)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinishReservationAsync_WhenValid_ShouldCompleteOpenOrder()
    {
        var reservation = new Reservation
        {
            Id = "r1",
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-20).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.MealsServed,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _repo.Setup(r => r.UpdateLifecycleAsync(
                reservation,
                ReservationStatus.MealsServed,
                ReservationStatus.Finished,
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepo.Setup(r => r.CompleteOnReservationFinishIfOpenAsync(
                "r1",
                "waiter-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.FinishReservationAsync("r1", "waiter-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.Finished);
        result.Value.ActualEndTime.Should().NotBeNullOrWhiteSpace();

        _orderRepo.Verify(r => r.CompleteOnReservationFinishIfOpenAsync(
            "r1",
            "waiter-1",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
