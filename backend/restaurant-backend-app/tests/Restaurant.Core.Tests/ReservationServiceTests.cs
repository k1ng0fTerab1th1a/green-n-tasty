using FluentAssertions;
using Moq;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.Core.Tests;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task GetMyAsync_WhenActorUserIdIsEmpty_ShouldReturnEmpty_AndNotCallRepo()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sut = new ReservationService(repo.Object);

        var result = await sut.GetMyAsync("", actorIsWaiter: false, ct: default);

        result.Should().BeEmpty();
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyAsync_WhenActorIsWaiter_ShouldQueryByWaiter()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.QueryByWaiterAsync("w1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c", WaiterId = "w1", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var sut = new ReservationService(repo.Object);

        var result = await sut.GetMyAsync("w1", actorIsWaiter: true, ct: default);

        result.Should().HaveCount(1);
        repo.Verify(r => r.QueryByWaiterAsync("w1", null, null, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyAsync_WhenActorIsCustomer_ShouldQueryByCustomer()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.QueryByCustomerAsync("c1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c1", WaiterId = "w", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var sut = new ReservationService(repo.Object);

        var result = await sut.GetMyAsync("c1", actorIsWaiter: false, ct: default);

        result.Should().HaveCount(1);
        repo.Verify(r => r.QueryByCustomerAsync("c1", null, null, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var sut = new ReservationService(repo.Object);

        var result = await sut.GetByIdAsync("r1", actorUserId: "c1", actorIsWaiter: false, ct: default);

        result.Should().BeNull();
        repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerOwner_ShouldReturnEntity()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
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

        var sut = new ReservationService(repo.Object);

        var result = await sut.GetByIdAsync("r1", actorUserId: "customerA", actorIsWaiter: false, ct: default);

        result.Should().NotBeNull();
        result!.Id.Should().Be("r1");
        repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenWaiterAssigned_ShouldReturnEntity()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
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

        var sut = new ReservationService(repo.Object);

        var result = await sut.GetByIdAsync("r1", actorUserId: "waiterA", actorIsWaiter: true, ct: default);

        result.Should().NotBeNull();
        repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotAllowed_ShouldThrow()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
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

        var sut = new ReservationService(repo.Object);

        var act = async () => await sut.GetByIdAsync("r1", actorUserId: "someoneElse", actorIsWaiter: false, ct: default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenActorIsDifferentWaiter_ShouldThrow()
    {
        var repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
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

        var sut = new ReservationService(repo.Object);

        var act = async () => await sut.GetByIdAsync("r1", actorUserId: "waiterB", actorIsWaiter: true, ct: default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Forbidden.");

        repo.Verify(r => r.GetByIdAsync("r1", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
