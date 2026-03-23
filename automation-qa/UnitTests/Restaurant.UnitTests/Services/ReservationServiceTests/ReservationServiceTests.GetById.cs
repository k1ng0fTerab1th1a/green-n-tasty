using FluentAssertions;
using Moq;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        _repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.GetByIdAsync("r1", actorUserId: "c1", actorIsWaiter: false, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.ReservationNotFound);
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

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("r1");
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

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotAllowed_ShouldReturnForbiddenError()
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

        var result = await _sut.GetByIdAsync("r1", actorUserId: "someoneElse", actorIsWaiter: false, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.Forbidden);
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenActorIsDifferentWaiter_ShouldReturnForbiddenError()
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

        var result = await _sut.GetByIdAsync("r1", actorUserId: "waiterB", actorIsWaiter: true, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.Forbidden);
        _repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }
}

