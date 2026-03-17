using FluentAssertions;
using Moq;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
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
}
