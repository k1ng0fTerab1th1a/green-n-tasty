using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

using FluentAssertions;
using Moq;
using Xunit;

public class FeedbackServiceTests
{
    [Fact]
    public async Task GetFeedbacksForLocation_ShouldCallRepository_AndReturnMappedDtos()
    {
        var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);

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

        repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                10,
                "waiter",
                It.IsAny<List<string>>(),
                null))
            .ReturnsAsync(repoResponse);

        var sut = new FeedbackService(repo.Object);

        var result = await sut.GetFeedbacksForLocation(
            "loc-1",
            10,
            "waiter",
            new List<string>());

        result.Size.Should().Be(10);
        result.NextPageToken.Should().Be("token-123");
        result.Content.Should().HaveCount(1);

        var dto = result.Content[0];
        dto.Id.Should().Be("fb-1");
        dto.Rate.Should().Be("5");
        dto.Comment.Should().Be("Great service");
        dto.LocationId.Should().Be("loc-1");
        dto.Type.Should().Be("waiter");
        dto.UserName.Should().Be("John");
        dto.UserAvatarUrl.Should().Be("avatar");

        repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            10,
            "waiter",
            It.IsAny<List<string>>(),
            null), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFeedbacksForLocation_WhenRepositoryReturnsEmpty_ShouldReturnEmptyContent()
    {
        var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);

        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>(),
            NextPageToken = null
        };

        repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                10,
                "waiter",
                It.IsAny<List<string>>(),
                null))
            .ReturnsAsync(repoResponse);

        var sut = new FeedbackService(repo.Object);

        var result = await sut.GetFeedbacksForLocation(
            "loc-1",
            10,
            "waiter",
            new List<string>());

        result.Size.Should().Be(10);
        result.Content.Should().BeEmpty();
        result.NextPageToken.Should().BeNull();

        repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            10,
            "waiter",
            It.IsAny<List<string>>(),
            null), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFeedbacksForLocation_ShouldForwardPageToken_ToRepository()
    {
        var repo = new Mock<IFeedbackRepository>(MockBehavior.Strict);

        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>(),
            NextPageToken = "next-token"
        };

        repo.Setup(r => r.GetByLocationAsync(
                "loc-1",
                5,
                "kitchen",
                It.IsAny<List<string>>(),
                "page-1"))
            .ReturnsAsync(repoResponse);

        var sut = new FeedbackService(repo.Object);

        var result = await sut.GetFeedbacksForLocation(
            "loc-1",
            5,
            "kitchen",
            new List<string>(),
            "page-1");

        result.Size.Should().Be(5);
        result.NextPageToken.Should().Be("next-token");
        result.Content.Should().BeEmpty();

        repo.Verify(r => r.GetByLocationAsync(
            "loc-1",
            5,
            "kitchen",
            It.IsAny<List<string>>(),
            "page-1"), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}