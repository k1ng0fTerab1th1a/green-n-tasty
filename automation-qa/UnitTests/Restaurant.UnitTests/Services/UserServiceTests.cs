using FluentAssertions;
using FluentResults;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed class UserServiceTests
{
    [Fact]
    public async Task GetMeAsync_WhenUserNotFound_ShouldReturnNotFound()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.GetMeAsync("ghost", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UserNotFound);

        userRepo.Verify(r => r.GetByIdAsync("ghost", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
        cognito.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMeAsync_WhenUserExists_ShouldReturnUser()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        var user = new User
        {
            UserId = "user-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@doe.com",
            Role = "CUSTOMER"
        };

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.GetMeAsync("user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(user);

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
        cognito.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenUserNotFound_ShouldReturnNotFound()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UserNotFound);

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenContentTypeInvalid_ShouldReturnValidationError()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "application/pdf", 16);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.InvalidFileType);

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenFileSignatureInvalid_ShouldReturnValidationError()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6 }), "image/png", 6);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.InvalidFileType);

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenFileTooBig_ShouldReturnValidationError()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 5 * 1024 * 1024 + 1);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.FileTooBig);

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenUploadFails_ShouldThrowException()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        fileService.Setup(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"))
            .ThrowsAsync(new Exception("Failed to upload to file system"));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        Func<Task> act = async () => await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("Failed to upload to file system");

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.Verify(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"), Times.Once);
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenValidImage_ShouldUploadAndPersistUrl()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        fileService.Setup(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"))
            .ReturnsAsync(Result.Ok());
        fileService.Setup(f => f.GetFileUrl("uploads/avatars/user-1/profile"))
            .Returns("https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile");

        userRepo.Setup(r => r.UpdateAvatarUrlAsync(
                "user-1",
                "https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile");

        userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.Verify(r => r.UpdateAvatarUrlAsync(
            "user-1",
            "https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile",
            It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();

        fileService.Verify(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"), Times.Once);
        fileService.Verify(f => f.GetFileUrl("uploads/avatars/user-1/profile"), Times.Once);
        fileService.VerifyNoOtherCalls();
    }
    
    // ── UpdateEmailAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEmailAsync_WhenCognitoFails_ShouldReturnCognitoError_AndNotUpdateRepository()
    {
        var cognito    = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo   = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UserErrors.UpdateNotSuccessful));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.UpdateEmailAsync("new@email.com", "access-token", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UpdateNotSuccessful);

        cognito.Verify(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateEmailAsync_WhenCognitoSucceeds_ShouldReturnOk_WithoutTouchingRepository()
    {
        var cognito     = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo    = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.UpdateEmailAsync("new@email.com", "access-token", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        cognito.Verify(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyEmailChangeAsync_WhenCodeInvalid_ShouldReturnError_AndNotUpdateRepository()
    {
        var cognito     = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo    = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.VerifyEmailChangeAsync("access-token", "wrong-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthErrors.InvalidVerificationCode));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.VerifyEmailChangeAsync("user-1", "new@email.com", "access-token", "wrong-code", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(AuthErrors.InvalidVerificationCode);

        cognito.Verify(c => c.VerifyEmailChangeAsync("access-token", "wrong-code", It.IsAny<CancellationToken>()), Times.Once);
        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyEmailChangeAsync_WhenCodeValid_ShouldUpdateRepository_AndReturnOk()
    {
        var cognito     = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo    = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.VerifyEmailChangeAsync("access-token", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        userRepo.Setup(r => r.UpdateEmailAsync("user-1", "new@email.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.VerifyEmailChangeAsync("user-1", "new@email.com", "access-token", "123456", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        cognito.Verify(c => c.VerifyEmailChangeAsync("access-token", "123456", It.IsAny<CancellationToken>()), Times.Once);
        cognito.VerifyNoOtherCalls();
        userRepo.Verify(r => r.UpdateEmailAsync("user-1", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    // ── UpdateUserNameAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserNameAsync_WhenRepositorySucceeds_ShouldReturnOk()
    {
        var cognito     = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo    = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.UpdateUserNameAsync("user-1", "Jane", "Smith", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        userRepo.Verify(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        cognito.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserNameAsync_WhenRepositoryThrows_ShouldReturnUpdateNotSuccessful()
    {
        var cognito     = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo    = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DynamoDB unavailable"));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.UpdateUserNameAsync("user-1", "Jane", "Smith", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UpdateNotSuccessful);

        userRepo.Verify(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()), Times.Once);
        userRepo.VerifyNoOtherCalls();
        cognito.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4, 5, 6, 7, 8];
}
