using FluentAssertions;
using Moq;
using Restaurant.Core.Models;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    [Fact]
    public async Task GetByCustomer_WhenActorUserIdIsEmpty_ShouldReturnEmpty_AndNotCallRepo()
    {
        var result = await _sut.GetByCustomer("", ct: default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByWaiter_WhenDateIsNull_ShouldQueryByWaiter()
    {
        _repo.Setup(r => r.QueryByWaiterAsync("w1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c", WaiterId = "w1", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var result = await _sut.GetByWaiter("w1", date: null, ct: default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _repo.Verify(r => r.QueryByWaiterAsync("w1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByWaiter_WhenDateProvided_ShouldQueryByWaiterWithDatePrefix()
    {
        _repo.Setup(r => r.QueryByWaiterAsync("w1", "2026-03-05", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c", WaiterId = "w1", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "2026-03-05T10:00+00:00", EndDateTime = "2026-03-05T11:00+00:00", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var result = await _sut.GetByWaiter("w1", new DateOnly(2026, 3, 5), ct: default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _repo.Verify(r => r.QueryByWaiterAsync("w1", "2026-03-05", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByCustomer_WhenActorIsCustomer_ShouldQueryByCustomer()
    {
        _repo.Setup(r => r.QueryByCustomerAsync("c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { new() { Id = "r1", CustomerId = "c1", WaiterId = "w", LocationId = "l", TableNumber = 1, TableKey = "l#1", StartDateTime = "s", EndDateTime = "e", GuestsCount = 2, CreatedAt = "c", UpdatedAt = "u" } });

        var result = await _sut.GetByCustomer("c1", ct: default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _repo.Verify(r => r.QueryByCustomerAsync("c1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }
}
