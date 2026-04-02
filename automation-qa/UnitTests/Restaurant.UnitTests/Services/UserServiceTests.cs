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
    public async Task UpdateAvatarAsync_WhenUploadFails_ShouldReturnUploadError()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        fileService.Setup(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"))
            .ReturnsAsync(Result.Fail(FileErrors.FileUploadFail));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        var result = await sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.FileUploadFail);

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

    [Fact]
    public async Task ChangePasswordAsync_WhenAccessTokenMissing_ShouldReturnUnauthorized_AndNotCallCognito()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.ChangePasswordAsync("", "OldPassword1", "NewPassword2", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(AuthErrors.AccessTokenRequired);

        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCognitoSucceeds_ShouldReturnOk_AndPassCorrectArguments()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.ChangePasswordAsync(
                "access-token-1",
                "OldPassword1",
                "NewPassword2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.ChangePasswordAsync(
            "access-token-1",
            "OldPassword1",
            "NewPassword2",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        cognito.Verify(c => c.ChangePasswordAsync(
            "access-token-1",
            "OldPassword1",
            "NewPassword2",
            It.IsAny<CancellationToken>()), Times.Once);

        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCognitoFails_ShouldPropagateFailure()
    {
        var cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var fileService = new Mock<IFileService>(MockBehavior.Strict);

        cognito.Setup(c => c.ChangePasswordAsync(
                "access-token-1",
                "WrongOldPassword1",
                "NewPassword2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthErrors.InvalidPasswordChangeRequest));

        var sut = new UserService(cognito.Object, userRepo.Object, fileService.Object);

        var result = await sut.ChangePasswordAsync(
            "access-token-1",
            "WrongOldPassword1",
            "NewPassword2",
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(AuthErrors.InvalidPasswordChangeRequest);

        cognito.Verify(c => c.ChangePasswordAsync(
            "access-token-1",
            "WrongOldPassword1",
            "NewPassword2",
            It.IsAny<CancellationToken>()), Times.Once);

        cognito.VerifyNoOtherCalls();
        userRepo.VerifyNoOtherCalls();
        fileService.VerifyNoOtherCalls();
    }

    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4, 5, 6, 7, 8];
}
