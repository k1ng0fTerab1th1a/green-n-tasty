using Microsoft.Extensions.Options;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using Restaurant.Core.SharedModels;

namespace Restaurant.UnitTests.Services;

using FluentAssertions;
using Moq;
using Xunit;

public class FeedbackServiceTests
{
    private readonly Mock<IFeedbackRepository> _repo;
    private readonly Mock<IReservationRepository> _resRepo;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<ILocationRepository> _locationRepo;
    private readonly FeedbackService _sut;

    public FeedbackServiceTests()
    {
        _repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);
        _resRepo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        
        var options = Options.Create(new ClientSettings
        {
            ClientUrl = "http://test-url.com"
        });

        _sut = new FeedbackService(
            _repo.Object,
            _resRepo.Object,
            _userRepo.Object,
            _locationRepo.Object,
            options
        );
    }

    [Fact]
    public async Task GetFeedbacksForLocation_ShouldCallRepository_AndReturnMappedDtos()
    {
        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>
            {
                new()
                {
                    Id = "fb-1",
                    Rate = 5,
                    Comment = "Great service",
                    Date = "2025-01-01",
                    Type = "waiter",
                    LocationId = "loc-1",
                    UserName = "John",
                    UserAvatarUrl = "avatar"
                }
            },
            NextPageToken = "token-123"
        };

        _repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                10,
                "waiter",
                It.IsAny<List<string>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResponse);

        var result = await _sut.GetFeedbacksForLocation(
            "loc-1",
            10,
            "waiter",
            new List<string>());

        result.Value.Size.Should().Be(10);
        result.Value.NextPageToken.Should().Be("token-123");
        result.Value.Content.Should().HaveCount(1);

        var dto = result.Value.Content[0];
        dto.Id.Should().Be("fb-1");
        dto.Rate.Should().Be("5");
        dto.Comment.Should().Be("Great service");
        dto.LocationId.Should().Be("loc-1");
        dto.Type.Should().Be("waiter");
        dto.UserName.Should().Be("John");
        dto.UserAvatarUrl.Should().Be("avatar");

        _repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            10,
            "waiter",
            It.IsAny<List<string>>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFeedbacksForLocation_WhenRepositoryReturnsEmpty_ShouldReturnEmptyContent()
    {
        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>(),
            NextPageToken = null
        };

        _repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                10,
                "waiter",
                It.IsAny<List<string>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResponse);

        var result = await _sut.GetFeedbacksForLocation(
            "loc-1",
            10,
            "waiter",
            new List<string>());

        result.Value.Size.Should().Be(10);
        result.Value.Content.Should().BeEmpty();
        result.Value.NextPageToken.Should().BeNull();

        _repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            10,
            "waiter",
            It.IsAny<List<string>>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFeedbacksForLocation_ShouldForwardPageToken_ToRepository()
    {
        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>(),
            NextPageToken = "next-token"
        };

        _repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                5,
                "kitchen",
                It.IsAny<List<string>>(),
                "page-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResponse);

        var result = await _sut.GetFeedbacksForLocation(
            "loc-1",
            5,
            "kitchen",
            new List<string>(),
            "page-1");

        result.Value.Size.Should().Be(5);
        result.Value.NextPageToken.Should().Be("next-token");
        result.Value.Content.Should().BeEmpty();

        _repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            5,
            "kitchen",
            It.IsAny<List<string>>(),
            "page-1",
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.VerifyNoOtherCalls();
    }
}