using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

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

        _sut = new FeedbackService(
            _repo.Object,
            _resRepo.Object,
            _userRepo.Object,
            _locationRepo.Object
        );
    }

    // ──────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────

    private void SetupFeedbackIdSave(string reservationId, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            _resRepo
                .Setup(r => r.SetFeedbackIdInReservation(
                    reservationId,
                    It.IsAny<string>(),
                    fieldName,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());
        }
    }

    private void SetupSaveServiceFeedback(Reservation reservation, int rating)
    {
        _repo.Setup(r => r.SaveFeedbackAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupFeedbackIdSave(reservation.Id, "serviceFeedbackId");

        _userRepo.Setup(u => u.UpdateUserRatingAsync(reservation.WaiterId, rating, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupSaveCuisineFeedback(Reservation reservation, int rating)
    {
        _repo.Setup(r => r.SaveFeedbackAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupFeedbackIdSave(reservation.Id, "kitchenFeedbackId");

        _locationRepo.Setup(l => l.UpdateKitchenRatingAsync(reservation.LocationId, rating, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ──────────────────────────────────────────────────────────
    //  GetFeedbacksForLocation
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFeedbacksForLocation_ShouldCallRepository_AndReturnMappedDtos()
    {
        var repoResponse = new FeedbackPaginatedDBResponseDto
        {
            Feedbacks = new List<Feedback>
            {
                new()
                {
                    Id            = "fb-1",
                    Rate          = 5,
                    Comment       = "Great service",
                    Date          = "2025-01-01",
                    Type          = "waiter",
                    LocationId    = "loc-1",
                    UserName      = "John",
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

    // ──────────────────────────────────────────────────────────
    //  Validation
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task SaveAuthorisedFeedback_WhenRatingOutOfRange_ShouldReturnValidationError(int rating)
    {
        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = rating };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.RatingValidationDiapasonError.Message);

        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenCommentTooLong_ShouldReturnValidationError()
    {
        var dto = new CreateFeedbackDTO
        {
            ReservationId  = "rsv-1",
            ServiceRating  = 5,
            ServiceComment = new string('x', 301)
        };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackCommentSizeOutOfBounds.Message);

        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    // ──────────────────────────────────────────────────────────
    //  SaveAuthorisedFeedback
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenReservationNotFound_ShouldReturnNotFoundError()
    {
        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation?)null);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.ReservationNotFound.Message);

        _resRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenCustomerIdMismatch_ShouldReturnUnauthorizedError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "different-user",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.ReservationUnauthorizedAccess.Message);

        _resRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenNeitherRatingProvided_ShouldReturnNoFeedbackProvidedError()
    {
        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1" };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.NoFeedbackProvided.Message);

        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenServiceFeedbackAlreadyMade_ShouldReturnAlreadyMadeError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            ServiceFeedbackId = "fb-service-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackAlreadyMade.Message);

        _resRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenCuisineFeedbackAlreadyMade_ShouldReturnAlreadyMadeError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            IsMealServed = true,
            KitchenFeedbackId = "fb-kitchen-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 4 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackAlreadyMade.Message);

        _resRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenServiceRating_AndStatusBelowInProgress_ShouldReturnTooEarlyError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.Reserved // below InProgress
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", (string?)null));

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.TooEarlyServiceFeedback.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenCuisineRating_AndMealNotServed_ShouldReturnMealNotServedError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress // below MealsServed
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", (string?)null));

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 4 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.MealNotYetServedForFeedback.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WithServiceRatingOnly_ShouldSaveFeedback_AndUpdateWaiterRating()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", "http://img/u1"));

        SetupSaveServiceFeedback(reservation, 5);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5, ServiceComment = "Excellent" };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f =>
                f.Type == "waiter" &&
                f.Rate == 5 &&
                f.Comment == "Excellent" &&
                f.UserId == "user-1" &&
                f.LocationId == "loc-1"),
            It.IsAny<CancellationToken>()), Times.Once);

        _userRepo.Verify(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WithBothRatings_ShouldSaveEachFeedbackSeparately_AndUpdateBothRatings()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            IsMealServed = true
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", (string?)null));

        SetupSaveServiceFeedback(reservation, 5);
        SetupSaveCuisineFeedback(reservation, 4);

        var dto = new CreateFeedbackDTO
        {
            ReservationId = "rsv-1",
            ServiceRating = 5,
            ServiceComment = "Great waiter",
            CuisineRating = 4,
            CuisineComment = "Tasty food"
        };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f => f.Type == "waiter" && f.Rate == 5),
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f => f.Type == "kitchen" && f.Rate == 4),
            It.IsAny<CancellationToken>()), Times.Once);

        _userRepo.Verify(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(l => l.UpdateKitchenRatingAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAuthorisedFeedback_WhenSetFeedbackIdFails_ShouldReturnFailure()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", (string?)null));

        _repo.Setup(r => r.SaveFeedbackAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        _resRepo.Setup(r => r.SetFeedbackIdInReservation("rsv-1", It.IsAny<string>(), "serviceFeedbackId", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Fail("Condition failed"));

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        _userRepo.Verify(u => u.UpdateUserRatingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────
    //  SaveVisitorFeedback
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveVisitorFeedback_WhenReservationNotFound_ShouldReturnNotFoundError()
    {
        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation?)null);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.ReservationNotFound.Message);

        _resRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenSecretCodeIsNull_ShouldReturnUnauthorizedError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            SecretCode = null
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.ReservationUnauthorizedAccess.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenSecretCodeMismatch_ShouldReturnUnauthorizedError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            SecretCode = "CORRECT-CODE"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveVisitorFeedback(dto, "WRONG-CODE", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.ReservationUnauthorizedAccess.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenNeitherRatingProvided_ShouldReturnNoFeedbackProvidedError()
    {
        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1" };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.NoFeedbackProvided.Message);

        _resRepo.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenServiceRating_AndStatusBelowInProgress_ShouldReturnTooEarlyError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.Reserved,
            SecretCode = "ALPHA-7X"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.TooEarlyServiceFeedback.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenCuisineRating_AndMealNotServed_ShouldReturnMealNotServedError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            IsMealServed = false,
            SecretCode = "ALPHA-7X"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 4 };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.MealNotYetServedForFeedback.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WithServiceRating_ShouldSaveFeedbackAsVisitor_AndClearSecretCode()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            SecretCode = "ALPHA-7X"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        SetupSaveServiceFeedback(reservation, 5);

        _resRepo.Setup(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5, ServiceComment = "Nice" };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f =>
                f.Type == "waiter" &&
                f.Rate == 5 &&
                f.UserName == "Visitor" &&
                f.UserId == string.Empty),
            It.IsAny<CancellationToken>()), Times.Once);

        _resRepo.Verify(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveVisitorFeedback_WithBothRatings_ShouldSaveBothFeedbacks_AndClearSecretCode()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            IsMealServed = true,
            SecretCode = "BRAVO-2K"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        SetupSaveServiceFeedback(reservation, 5);
        SetupSaveCuisineFeedback(reservation, 4);

        _resRepo.Setup(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var dto = new CreateFeedbackDTO
        {
            ReservationId = "rsv-1",
            ServiceRating = 5,
            CuisineRating = 4,
            CuisineComment = "Good food"
        };

        var result = await _sut.SaveVisitorFeedback(dto, "BRAVO-2K", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f => f.Type == "waiter" && f.UserName == "Visitor"),
            It.IsAny<CancellationToken>()), Times.Once);

        _repo.Verify(r => r.SaveFeedbackAsync(
            It.Is<Feedback>(f => f.Type == "kitchen" && f.UserName == "Visitor"),
            It.IsAny<CancellationToken>()), Times.Once);

        _resRepo.Verify(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _locationRepo.Verify(l => l.UpdateKitchenRatingAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveVisitorFeedback_WhenProcessFails_ShouldNotClearSecretCode()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.Reserved,
            SecretCode = "ALPHA-7X"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        _resRepo.Verify(r => r.ClearSecretCode(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────
    //  GetWaiterLocationFeedbackDtoAsync
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWaiterLocationFeedbackDtoAsync_WhenReservationNotFound_ShouldReturnNotFoundError()
    {
        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation?)null);

        var result = await _sut.GetWaiterLocationFeedbackDtoAsync("rsv-1", false, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.ReservationNotFound.Message);
    }

    [Fact]
    public async Task GetWaiterLocationFeedbackDtoAsync_ShouldCalculateAverageRatings_Correctly()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _locationRepo.Setup(l => l.GetLocationFeedbacksDataAsync("loc-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((rating: 18, feedbacksAmount: 4)); // avg = 4.5

        _userRepo.Setup(u => u.GetWaiterFeedbackDataAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new WaiterFeedbackData { WaiterName = "Alice", WaiterRating = 24, WaiterFeedbacksNumber = 5 }); // avg = 4.8

        var result = await _sut.GetWaiterLocationFeedbackDtoAsync("rsv-1", false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CuisineRating.Should().BeApproximately(4.5, 0.001);
        result.Value.CuisineFeedbacksNumber.Should().Be(4);
        result.Value.WaiterRating.Should().BeApproximately(4.8, 0.001);
        result.Value.WaiterFeedbacksNumber.Should().Be(5);
        result.Value.WaiterName.Should().Be("Alice");
        result.Value.UpdateUserData.Should().BeNull();
    }

    [Fact]
    public async Task GetWaiterLocationFeedbackDtoAsync_WhenNoFeedbacksExist_ShouldReturnZeroRatings()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _locationRepo.Setup(l => l.GetLocationFeedbacksDataAsync("loc-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((rating: 0, feedbacksAmount: 0));

        _userRepo.Setup(u => u.GetWaiterFeedbackDataAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new WaiterFeedbackData { WaiterRating = 0, WaiterFeedbacksNumber = 0 });

        var result = await _sut.GetWaiterLocationFeedbackDtoAsync("rsv-1", false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CuisineRating.Should().Be(0);
        result.Value.WaiterRating.Should().Be(0);
    }

    [Fact]
    public async Task GetWaiterLocationFeedbackDtoAsync_WhenIsForUpdate_AndBothFeedbacksExist_ShouldPopulateUpdateUserData()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            KitchenFeedbackId = "kf-1",
            ServiceFeedbackId = "sf-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _locationRepo.Setup(l => l.GetLocationFeedbacksDataAsync("loc-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((rating: 10, feedbacksAmount: 2));

        _userRepo.Setup(u => u.GetWaiterFeedbackDataAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new WaiterFeedbackData { WaiterRating = 8, WaiterFeedbacksNumber = 2 });

        _repo.Setup(r => r.GetByIdAsync("kf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "kf-1", Rate = 5, Comment = "Great food" });

        _repo.Setup(r => r.GetByIdAsync("sf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "sf-1", Rate = 4, Comment = "Good service" });

        var result = await _sut.GetWaiterLocationFeedbackDtoAsync("rsv-1", true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpdateUserData.Should().NotBeNull();
        result.Value.UpdateUserData!.KitchenFeedbackId.Should().Be("kf-1");
        result.Value.UpdateUserData.KitchenRating.Should().Be(5);
        result.Value.UpdateUserData.KitchenComment.Should().Be("Great food");
        result.Value.UpdateUserData.ServiceFeedbackId.Should().Be("sf-1");
        result.Value.UpdateUserData.ServiceRating.Should().Be(4);
        result.Value.UpdateUserData.ServiceComment.Should().Be("Good service");
    }

    [Fact]
    public async Task GetWaiterLocationFeedbackDtoAsync_WhenIsForUpdateFalse_ShouldNotFetchFeedbacks()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            KitchenFeedbackId = "kf-1",
            ServiceFeedbackId = "sf-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _locationRepo.Setup(l => l.GetLocationFeedbacksDataAsync("loc-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((rating: 10, feedbacksAmount: 2));

        _userRepo.Setup(u => u.GetWaiterFeedbackDataAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new WaiterFeedbackData { WaiterRating = 8, WaiterFeedbacksNumber = 2 });

        var result = await _sut.GetWaiterLocationFeedbackDtoAsync("rsv-1", false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpdateUserData.Should().BeNull();
        _repo.VerifyNoOtherCalls();
    }

    // ──────────────────────────────────────────────────────────
    //  UpdateFeedback
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateFeedback_WhenNeitherRatingProvided_ShouldReturnNoFeedbackUpdatedError()
    {
        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1" };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.NoFeedbackUpdated.Message);

        _resRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFeedback_WhenReservationNotFound_ShouldReturnNotFoundError()
    {
        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation?)null);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 4 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.ReservationNotFound.Message);
    }

    [Fact]
    public async Task UpdateFeedback_WhenCustomerIdMismatch_ShouldReturnUnauthorizedError()
    {
        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Reservation
                {
                    Id = "rsv-1",
                    CustomerId = "other-user",
                    WaiterId = "w-1",
                    LocationId = "loc-1"
                });

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 4 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.ReservationUnauthorizedAccess.Message);

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateFeedback_WhenExistingServiceFeedback_ShouldRecalculateRating_AndUpdateBoth()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            ServiceFeedbackId = "sf-1",
            CustomerId = "user-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _repo.Setup(r => r.GetByIdAsync("sf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "sf-1", Rate = 3, Comment = "Okay" });

        _userRepo.Setup(u => u.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new User { UserId = "waiter-1", TotalRating = 15, FeedbacksCount = 5 });

        _userRepo.Setup(u => u.CreateAsync(
                It.Is<User>(w => w.TotalRating == 17), // 15 - 3 + 5 = 17
                It.IsAny<CancellationToken>(), true))
            .Returns(Task.CompletedTask);

        _repo.Setup(r => r.UpdateFeedback(
                It.Is<Feedback>(f => f.Rate == 5 && f.Comment == "Updated"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5, ServiceComment = "Updated" };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _userRepo.Verify(u => u.CreateAsync(
            It.Is<User>(w => w.TotalRating == 17),
            It.IsAny<CancellationToken>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateFeedback_WhenExistingKitchenFeedback_ShouldRecalculateRating_AndUpdateBoth()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            KitchenFeedbackId = "kf-1",
            CustomerId = "user-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _repo.Setup(r => r.GetByIdAsync("kf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "kf-1", Rate = 2, Comment = "Bad" });

        _locationRepo.Setup(l => l.GetByIdAsync("loc-1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Location { Id = "loc-1", TotalRating = 10 });

        _locationRepo.Setup(l => l.UpdateAsync(
                It.Is<Location>(loc => loc.TotalRating == 13), // 10 - 2 + 5 = 13
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repo.Setup(r => r.UpdateFeedback(
                It.Is<Feedback>(f => f.Rate == 5 && f.Comment == "Much better"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 5, CuisineComment = "Much better" };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _locationRepo.Verify(l => l.UpdateAsync(
            It.Is<Location>(loc => loc.TotalRating == 13),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeedback_WhenNoExistingServiceFeedback_ShouldCreateNewOne()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            CustomerId = "user-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            Status = ReservationStatus.InProgress,
            ServiceFeedbackId = null
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _userRepo.Setup(u => u.GetUserDataForFeedbackCreationByIdAsync("user-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("Ana K.", (string?)null));

        SetupSaveServiceFeedback(reservation, 4);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 4 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.SaveFeedbackAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeedback_WhenServiceFeedbackNotFound_ShouldReturnFeedbackNotFoundError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            ServiceFeedbackId = "sf-missing",
            CustomerId = "user-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _repo.Setup(r => r.GetByIdAsync("sf-missing", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Feedback?)null);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 4 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackNotFound.Message);
    }

    [Fact]
    public async Task UpdateFeedback_WhenWaiterNotFound_ShouldReturnWaiterNotFoundError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-missing",
            LocationId = "loc-1",
            ServiceFeedbackId = "sf-1",
            CustomerId = "user-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _repo.Setup(r => r.GetByIdAsync("sf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "sf-1", Rate = 3 });

        _userRepo.Setup(u => u.GetByIdAsync("waiter-missing", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 4 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(ReservationErrors.WaiterNotFound.Message);
    }

    [Fact]
    public async Task UpdateFeedback_WhenUpdateThrows_ShouldReturnFeedbackUpdateUnsuccessfulError()
    {
        var reservation = new Reservation
        {
            Id = "rsv-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            ServiceFeedbackId = "sf-1",
            CustomerId = "user-1"
        };

        _resRepo.Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

        _repo.Setup(r => r.GetByIdAsync("sf-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Feedback { Id = "sf-1", Rate = 3 });

        _userRepo.Setup(u => u.GetByIdAsync("waiter-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new User { UserId = "waiter-1", TotalRating = 10 });

        _userRepo.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>(), true))
                 .ThrowsAsync(new Exception("DynamoDB unavailable"));

        _repo.Setup(r => r.UpdateFeedback(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

        var result = await _sut.UpdateFeedback(dto, "user-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackUpdateUnsuccessful.Message);
    }
}
