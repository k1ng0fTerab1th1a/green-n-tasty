using FluentAssertions;
using Moq;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
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
}
