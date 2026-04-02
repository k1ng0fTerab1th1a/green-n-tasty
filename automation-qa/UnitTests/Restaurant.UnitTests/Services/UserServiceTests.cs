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
    private readonly Mock<ICognitoService> _cognito;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IFileService> _fileService;
    private readonly Mock<IEmailService> _emailService;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        _userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _fileService = new Mock<IFileService>(MockBehavior.Strict);
        _emailService = new Mock<IEmailService>(MockBehavior.Strict);
        _sut = new UserService(_cognito.Object, _userRepo.Object, _fileService.Object, _emailService.Object);
    }
    [Fact]
    public async Task GetMeAsync_WhenUserNotFound_ShouldReturnNotFound()
    {

        _userRepo.Setup(r => r.GetByIdAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetMeAsync("ghost", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UserNotFound);

        _userRepo.Verify(r => r.GetByIdAsync("ghost", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
        _cognito.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMeAsync_WhenUserExists_ShouldReturnUser()
    {

        var user = new User
        {
            UserId = "user-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@doe.com",
            Role = "CUSTOMER"
        };

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetMeAsync("user-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(user);

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
        _cognito.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenUserNotFound_ShouldReturnNotFound()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        var result = await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UserNotFound);

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenContentTypeInvalid_ShouldReturnValidationError()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "application/pdf", 16);

        var result = await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.InvalidFileType);

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenFileSignatureInvalid_ShouldReturnValidationError()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });
        var dto = new FileUploadDto(new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6 }), "image/png", 6);

        var result = await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.InvalidFileType);

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenFileTooBig_ShouldReturnValidationError()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 5 * 1024 * 1024 + 1);

        var result = await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(FileErrors.FileTooBig);

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenUploadFails_ShouldThrowException()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        _fileService.Setup(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"))
            .ThrowsAsync(new Exception("Failed to upload to file system"));
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        Func<Task> act = async () => await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("Failed to upload to file system");

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.Verify(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"), Times.Once);
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenValidImage_ShouldUploadAndPersistUrl()
    {

        _userRepo.Setup(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserId = "user-1" });

        _fileService.Setup(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"))
            .ReturnsAsync(Result.Ok());
        _fileService.Setup(f => f.GetFileUrl("uploads/avatars/user-1/profile"))
            .Returns("https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile");

        _userRepo.Setup(r => r.UpdateAvatarUrlAsync(
                "user-1",
                "https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dto = new FileUploadDto(new MemoryStream(PngBytes()), "image/png", 16);

        var result = await _sut.UpdateAvatarAsync("user-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile");

        _userRepo.Verify(r => r.GetByIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.Verify(r => r.UpdateAvatarUrlAsync(
            "user-1",
            "https://bucket.s3.amazonaws.com/uploads/avatars/user-1/profile",
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();

        _fileService.Verify(f => f.UploadFileAsync("uploads/avatars/user-1/profile", It.IsAny<Stream>(), "image/png"), Times.Once);
        _fileService.Verify(f => f.GetFileUrl("uploads/avatars/user-1/profile"), Times.Once);
        _fileService.VerifyNoOtherCalls();
    }
    
    // ── UpdateEmailAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEmailAsync_WhenCognitoFails_ShouldReturnCognitoError_AndNotUpdateRepository()
    {

        _cognito.Setup(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UserErrors.UpdateNotSuccessful));

        var result = await _sut.UpdateEmailAsync("new@email.com", "access-token", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UpdateNotSuccessful);

        _cognito.Verify(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        _cognito.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateEmailAsync_WhenCognitoSucceeds_ShouldReturnOk_WithoutTouchingRepository()
    {

        _cognito.Setup(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var result = await _sut.UpdateEmailAsync("new@email.com", "access-token", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _cognito.Verify(c => c.UpdateUserEmailAsync("access-token", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        _cognito.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyEmailChangeAsync_WhenCodeInvalid_ShouldReturnError_AndNotUpdateRepository()
    {

        _cognito.Setup(c => c.VerifyEmailChangeAsync("access-token", "wrong-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthErrors.InvalidVerificationCode));

        var result = await _sut.VerifyEmailChangeAsync("user-1", "new@email.com", "access-token", "wrong-code", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(AuthErrors.InvalidVerificationCode);

        _cognito.Verify(c => c.VerifyEmailChangeAsync("access-token", "wrong-code", It.IsAny<CancellationToken>()), Times.Once);
        _cognito.VerifyNoOtherCalls();
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyEmailChangeAsync_WhenCodeValid_ShouldUpdateRepository_AndReturnOk()
    {

        _cognito.Setup(c => c.VerifyEmailChangeAsync("access-token", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        _userRepo.Setup(r => r.UpdateEmailAsync("user-1", "new@email.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.VerifyEmailChangeAsync("user-1", "new@email.com", "access-token", "123456", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _cognito.Verify(c => c.VerifyEmailChangeAsync("access-token", "123456", It.IsAny<CancellationToken>()), Times.Once);
        _cognito.VerifyNoOtherCalls();
        _userRepo.Verify(r => r.UpdateEmailAsync("user-1", "new@email.com", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    // ── UpdateUserNameAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserNameAsync_WhenRepositorySucceeds_ShouldReturnOk()
    {

        _userRepo.Setup(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateUserNameAsync("user-1", "Jane", "Smith", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _userRepo.Verify(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _cognito.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserNameAsync_WhenRepositoryThrows_ShouldReturnUpdateNotSuccessful()
    {

        _userRepo.Setup(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DynamoDB unavailable"));

        var result = await _sut.UpdateUserNameAsync("user-1", "Jane", "Smith", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(UserErrors.UpdateNotSuccessful);

        _userRepo.Verify(r => r.UpdateUserNameAsync("user-1", "Jane", "Smith", It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.VerifyNoOtherCalls();
        _cognito.VerifyNoOtherCalls();
        _fileService.VerifyNoOtherCalls();
    }

    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4, 5, 6, 7, 8];
}
