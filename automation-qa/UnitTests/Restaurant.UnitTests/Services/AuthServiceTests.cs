using FluentAssertions;
using FluentResults;
using Moq;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Restaurant.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<ICognitoService> _cognito;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IWaiterListRepository> _waiterListRepo;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _cognito = new Mock<ICognitoService>(MockBehavior.Strict);
        _userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _waiterListRepo = new Mock<IWaiterListRepository>(MockBehavior.Strict);

        _sut = new AuthService(_cognito.Object, _userRepo.Object, _waiterListRepo.Object);
    }

    [Fact]
    public async Task SignUp_ShouldAssignCustomerRole_WhenEmailNotInWaiterList()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _cognito.Setup(c => c.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("user-id-123"));
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cognito.Verify(c => c.SignUpAsync(
            "user@test.com",
            "Pass123!",
            "John",
            "Doe",
            "CUSTOMER",
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SignUp_ShouldAssignWaiterRole_WhenEmailInWaiterList()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("waiter@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cognito.Setup(c => c.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith", "WAITER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("waiter-id-456"));
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cognito.Verify(c => c.SignUpAsync(
            "waiter@test.com",
            "Pass123!",
            "Bob",
            "Smith",
            "WAITER",
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SignUp_ShouldSaveUser_WithCorrectData()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _cognito.Setup(c => c.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("user-id-123"));
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.CreateAsync(
            It.Is<User>(u =>
                u.UserId == "user-id-123" &&
                u.Email == "user@test.com" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.Role == "CUSTOMER" &&
                u.WaiterFlag == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignUp_ShouldSetWaiterFlag_WhenEmailInWaiterList()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("waiter@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cognito.Setup(c => c.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith", "WAITER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("waiter-id-456"));
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.CreateAsync(
            It.Is<User>(u =>
                u.UserId == "waiter-id-456" &&
                u.Email == "waiter@test.com" &&
                u.Role == "WAITER" &&
                u.WaiterFlag == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignUp_ShouldRollbackCognito_WhenDynamoFails()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _cognito.Setup(c => c.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("user-id-123"));
        _cognito.Setup(c => c.DeleteUserAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());

        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DynamoDB unavailable"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        _cognito.Verify(c => c.DeleteUserAsync("user@test.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignUp_WhenUserAlreadyExists_ShouldReturnFailedResult()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _cognito.Setup(c => c.SignUpAsync(
                "user@test.com",
                "Pass123!",
                "John",
                "Doe",
                "CUSTOMER",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<string>(AuthErrors.UserAlreadyExists));

        // Act
        var result = await _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.UserAlreadyExists);

        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignUp_ShouldNotSaveUser_WhenCognitoFails()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _cognito.Setup(c => c.SignUpAsync(
                "user@test.com",
                "Pass123!",
                "John",
                "Doe",
                "CUSTOMER",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cognito error"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _cognito.Verify(c => c.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignUp_ShouldNotCallCognito_WhenWaiterListFails()
    {
        // Arrange
        _waiterListRepo.Setup(r => r.ContainsAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Waiter list unavailable"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        _cognito.Verify(c => c.SignUpAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignIn_ShouldReturnCorrectUsernameAndRole_FromJwtClaims()
    {
        var fakeToken = GenerateFakeJwt("John", "Doe", "CUSTOMER");

        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(string, string)>((fakeToken, "refresh-token")));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Value.Username.Should().Be("John Doe");
        result.Value.Role.Should().Be("CUSTOMER");
        result.Value.IdToken.Should().Be(fakeToken);
        result.Value.RefreshToken.Should().Be("refresh-token");
    }



    [Fact]
    public async Task SignIn_ShouldReturnWaiterRole_WhenTokenContainsWaiterRole()
    {
        var fakeToken = GenerateFakeJwt("Bob", "Smith", "WAITER");
        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(string, string)>((fakeToken, "refresh-token")));

        var result = await _sut.SignInAsync("waiter@test.com", "Pass123!");

        result.Value.Role.Should().Be("WAITER");
    }

    [Fact]
    public async Task SignIn_ShouldReturnCustomerRole_WhenRoleClaimMissing()
    {
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim("given_name", "John"),
            new Claim("family_name", "Doe")
        });
        var fakeToken = new JwtSecurityTokenHandler().WriteToken(token);

        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(string, string)>((fakeToken, "refresh-token")));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Value.Role.Should().Be("CUSTOMER");
    }

    [Fact]
    public async Task SignIn_ShouldReturnEmptyUsername_WhenNameClaimsMissing()
    {
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim("custom:role", "CUSTOMER")
        });
        var fakeToken = new JwtSecurityTokenHandler().WriteToken(token);

        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(string, string)>((fakeToken, "refresh-token")));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Value.Username.Should().BeEmpty();
    }

    [Fact]
    public async Task SignIn_WhenInvalidCredentials_ShouldReturnFailedResult()
    {
        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<(string, string)>(AuthErrors.InvalidCredentials));

        var result = await _sut.SignInAsync("user@test.com", "wrong");

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task SignIn_ShouldPropagate_EmailNotVerified_WhenCognitoReturnsEmailNotVerified()
    {
        _cognito.Setup(c => c.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<(string, string)>(AuthErrors.EmailNotVerified));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.EmailNotVerified);
    }

    private static string GenerateFakeJwt(string firstName, string lastName, string role)
    {
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim("given_name", firstName),
            new Claim("family_name", lastName),
            new Claim("custom:role", role)
        });

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

