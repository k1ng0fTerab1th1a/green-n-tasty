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

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1" }; // no ratings

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.NoFeedbackProvided.Message);

    _repo.VerifyNoOtherCalls();
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
        Status = ReservationStatus.Reserved   // below InProgress
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
public async Task SaveAuthorisedFeedback_WhenServiceFeedbackAlreadyMade_ShouldReturnAlreadyMadeError()
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

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackAlreadyMade.Message);
}

[Fact]
public async Task SaveAuthorisedFeedback_WhenWaiterRatingDataInvalid_ShouldStillSaveFeedback()
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
}

[Fact]
public async Task SaveAuthorisedFeedback_WhenCuisineRating_AndStatusBelowMealsServed_ShouldReturnMealNotServedError()
{
    var reservation = new Reservation
    {
        Id = "rsv-1",
        CustomerId = "user-1",
        WaiterId = "waiter-1",
        LocationId = "loc-1",
        Status = ReservationStatus.InProgress  // below MealsServed
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
public async Task SaveAuthorisedFeedback_WhenCuisineFeedbackAlreadyMade_ShouldReturnAlreadyMadeError()
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

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 4 };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.FeedbackAlreadyMade.Message);
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5, ServiceComment = "Excellent" };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();

    _repo.Verify(r => r.SaveBatchAsync(
        It.Is<IEnumerable<Feedback>>(list =>
            list.Count() == 1 &&
            list.First().Type == "waiter" &&
            list.First().Rate == 5 &&
            list.First().Comment == "Excellent" &&
            list.First().UserId == "user-1" &&
            list.First().LocationId == "loc-1"),
        It.IsAny<CancellationToken>()), Times.Once);

    _userRepo.Verify(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()), Times.Once);
    _locationRepo.VerifyNoOtherCalls();
}

[Fact]
public async Task SaveAuthorisedFeedback_WithBothRatings_ShouldSaveTwoFeedbacks_AndUpdateBothRatings()
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

    _locationRepo.Setup(l => l.UpdateKitchenRatingAsync("loc-1", 4, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO
    {
        ReservationId  = "rsv-1",
        ServiceRating  = 5,
        ServiceComment = "Great waiter",
        CuisineRating  = 4,
        CuisineComment = "Tasty food"
    };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();

    _repo.Verify(r => r.SaveBatchAsync(
        It.Is<IEnumerable<Feedback>>(list =>
            list.Count() == 2 &&
            list.Any(f => f.Type == "waiter" && f.Rate == 5) &&
            list.Any(f => f.Type == "kitchen" && f.Rate == 4)),
        It.IsAny<CancellationToken>()), Times.Once);

    _userRepo.Verify(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()), Times.Once);
    _locationRepo.Verify(l => l.UpdateKitchenRatingAsync("loc-1", 4, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task SaveAuthorisedFeedback_WhenRatingUpdateThrows_ShouldReturnUnsuccessfulRatingUpdateError()
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("DynamoDB unavailable"));

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

    var result = await _sut.SaveAuthorisedFeedback(dto, "user-1", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.UnsuccessfulRatingUpdate.Message);
}



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
public async Task SaveVisitorFeedback_WhenSecretCodeIsEmpty_ShouldReturnUnauthorizedError()
{
    var reservation = new Reservation
    {
        Id = "rsv-1",
        CustomerId = null,
        WaiterId = "waiter-1",
        LocationId = "loc-1",
        Status = ReservationStatus.InProgress,
        SecretCode = null   // no secret set
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
        CustomerId = null,
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

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1" }; // no ratings

    var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.NoFeedbackProvided.Message);

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
public async Task SaveVisitorFeedback_WhenCuisineRating_AndStatusBelowMealsServed_ShouldReturnMealNotServedError()
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

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", CuisineRating = 4 };

    var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.MealNotYetServedForFeedback.Message);

    _repo.VerifyNoOtherCalls();
}

[Fact]
public async Task SaveVisitorFeedback_WithServiceRating_ShouldSaveFeedback()
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    _resRepo.Setup(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

    var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();

    _repo.Verify(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()), Times.Once);
    _resRepo.Verify(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task SaveVisitorFeedback_WithServiceRatingOnly_ShouldSaveFeedbackAsVisitor_AndClearSecretCode()
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


    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

    _resRepo.Setup(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5, ServiceComment = "Nice" };

    var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();

    _repo.Verify(r => r.SaveBatchAsync(
        It.Is<IEnumerable<Feedback>>(list =>
            list.Count() == 1 &&
            list.First().Type == "waiter" &&
            list.First().Rate == 5 &&
            list.First().UserName == "Visitor" &&
            list.First().UserId == string.Empty),
        It.IsAny<CancellationToken>()), Times.Once);

    _resRepo.Verify(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
    _locationRepo.VerifyNoOtherCalls();
}

[Fact]
public async Task SaveVisitorFeedback_WithBothRatings_ShouldSaveTwoFeedbacks_AndClearSecretCode()
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


    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", 5, It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

    _locationRepo.Setup(l => l.UpdateKitchenRatingAsync("loc-1", 4, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

    _resRepo.Setup(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    var dto = new CreateFeedbackDTO
    {
        ReservationId  = "rsv-1",
        ServiceRating  = 5,
        CuisineRating  = 4,
        CuisineComment = "Good food"
    };

    var result = await _sut.SaveVisitorFeedback(dto, "BRAVO-2K", CancellationToken.None);

    result.IsSuccess.Should().BeTrue();

    _repo.Verify(r => r.SaveBatchAsync(
        It.Is<IEnumerable<Feedback>>(list =>
            list.Count() == 2 &&
            list.All(f => f.UserName == "Visitor") &&
            list.Any(f => f.Type == "waiter") &&
            list.Any(f => f.Type == "kitchen")),
        It.IsAny<CancellationToken>()), Times.Once);

    _resRepo.Verify(r => r.ClearSecretCode("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task SaveVisitorFeedback_WhenRatingUpdateThrows_ShouldReturnUnsuccessfulRatingUpdateError_AndNotClearSecretCode()
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

    _repo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Feedback>>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

    _userRepo.Setup(u => u.UpdateUserRatingAsync("waiter-1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("DynamoDB unavailable"));

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-1", ServiceRating = 5 };

    var result = await _sut.SaveVisitorFeedback(dto, "ALPHA-7X", CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors[0].Message.Should().Be(FeedbackErrors.UnsuccessfulRatingUpdate.Message);

    _resRepo.Verify(r => r.ClearSecretCode(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
}
}