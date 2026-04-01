using FluentAssertions;
using Moq;
using Restaurant.Core.Errors;
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
                TotalRating = 400,
                FeedbacksAmount = 100 
            }
        };

        repo.Setup(r => r.GetLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationsAsync(CancellationToken.None);

        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be("loc-1");

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
                TotalRating = 464,
                FeedbacksAmount = 100 
            }
        };

        repo.Setup(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetLocationOptionsAsync(CancellationToken.None);

        result.Value.Should().HaveCount(1);
        result.Value[0].Address.Should().Be("Main street 1");

        repo.Verify(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenLocationExists_ShouldCallRepository_AndReturnLocation()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        var expected = new Location
        {
            Id = "loc-42",
            Address = "Berlin, Test str 1",
            Description = "Panoramic hall",
            TotalCapacity = 120,
            AverageOccupancy = 0.354,
            ImageUrl = "http://img/loc-42",
            TotalRating = 464,
            FeedbacksAmount = 100 
        };

        repo.Setup(r => r.GetByIdAsync("loc-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetByIdAsync("loc-42", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("loc-42");
        result.Value.Address.Should().Be("Berlin, Test str 1");

        repo.Verify(r => r.GetByIdAsync("loc-42", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_WhenLocationDoesNotExist_ShouldReturnFailedResult()
    {
        var repo = new Mock<ILocationRepository>(MockBehavior.Strict);

        repo.Setup(r => r.GetByIdAsync("missing-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var sut = new LocationService(repo.Object);

        var result = await sut.GetByIdAsync("missing-id", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<BusinessError>();
        ((BusinessError)result.Errors[0]).Type.Should().Be(ErrorType.NotFound);

        repo.Verify(r => r.GetByIdAsync("missing-id", It.IsAny<CancellationToken>()), Times.Once);
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

        result.Value.Should().BeEmpty();
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

        result.Value.Should().BeEmpty();
        repo.Verify(r => r.GetLocationOptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}