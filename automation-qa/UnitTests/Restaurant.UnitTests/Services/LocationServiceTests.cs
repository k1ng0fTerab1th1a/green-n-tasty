using FluentAssertions;
using Moq;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed class LocationServiceTests
{
    [Fact]
    public async Task GetLocationsAsync_ShouldCallRepository_AndReturnItems()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        var expected = new List<Location>
    {
        new()
        {
            Id = "loc-1",
            Address = "Main street 1",
            Description = "Test",
            TotalCapacity = 120,
            AverageOccupancy = 0.35,
            ImageUrl = "http://img",
            Rating = 4.6
        }
    };

        repo.Setup(r => r.GetLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationsAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("loc-1");

        repo.Verify(r => r.GetLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationOptionsAsync_ShouldCallRepository_AndReturnItems()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        var expected = new List<Location>
    {
        new()
        {
            Id = "loc-1",
            Address = "Main street 1",
            Description = "Test",
            TotalCapacity = 120,
            AverageOccupancy = 0.35,
            ImageUrl = "http://img",
            Rating = 4.6
        }
    };

        repo.Setup(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationOptionsAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Address.Should().Be("Main street 1");

        repo.Verify(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationsAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmpty()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        repo.Setup(r => r.GetLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Location>());

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationsAsync(CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationOptionsAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmpty()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        repo.Setup(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Location>());

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationOptionsAsync(CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
