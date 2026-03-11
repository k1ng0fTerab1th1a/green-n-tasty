using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Restaurant.UnitTests.Services;

public class AuthServiceTests
{
    private readonly ICognitoService _cognito;
    private readonly IUserRepository _userRepo;
    private readonly IWaiterListRepository _waiterListRepo;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _cognito = Substitute.For<ICognitoService>();
        _userRepo = Substitute.For<IUserRepository>();
        _waiterListRepo = Substitute.For<IWaiterListRepository>();

        _sut = new AuthService(_cognito, _userRepo, _waiterListRepo);
    }

    [Fact]
    public async Task SignUp_ShouldAssignCustomerRole_WhenEmailNotInWaiterList()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("user@test.com").Returns(false);
        _cognito.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER")
            .Returns("user-id-123");

        // Act
        await _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await _cognito.Received(1).SignUpAsync(
            "user@test.com",
            "Pass123!",
            "John",
            "Doe",
            "CUSTOMER");
    }

    [Fact]
    public async Task SignUp_ShouldAssignWaiterRole_WhenEmailInWaiterList()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("waiter@test.com").Returns(true);
        _cognito.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith", "WAITER")
            .Returns("waiter-id-456");

        // Act
        await _sut.SignUpAsync("waiter@test.com", "Pass123!", "Bob", "Smith");

        // Assert
        await _cognito.Received(1).SignUpAsync(
            "waiter@test.com",
            "Pass123!",
            "Bob",
            "Smith",
            "WAITER");
    }

    [Fact]
    public async Task SignUp_ShouldSaveUser_WithCorrectData()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("user@test.com").Returns(false);
        _cognito.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER")
            .Returns("user-id-123");

        // Act
        await _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await _userRepo.Received(1).CreateAsync(
            Arg.Is<User>(u =>
                u.UserId == "user-id-123" &&
                u.Email == "user@test.com" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.Role == "CUSTOMER"));
    }

    [Fact]
    public async Task SignUp_ShouldRollbackCognito_WhenDynamoFails()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("user@test.com").Returns(false);
        _cognito.SignUpAsync("user@test.com", "Pass123!", "John", "Doe", "CUSTOMER")
            .Returns("user-id-123");

        _userRepo.CreateAsync(Arg.Any<User>())
            .Throws(new Exception("DynamoDB unavailable"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        await _cognito.Received(1).DeleteUserAsync("user@test.com");
    }

    [Fact]
    public async Task SignUp_ShouldNotSaveUser_WhenCognitoFails()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("user@test.com").Returns(false);

        _cognito.SignUpAsync(
            "user@test.com",
            "Pass123!",
            "John",
            "Doe",
            "CUSTOMER")
            .Throws(new Exception("Cognito error"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        await _userRepo.DidNotReceive().CreateAsync(Arg.Any<User>());
        await _cognito.DidNotReceive().DeleteUserAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SignUp_ShouldNotCallCognito_WhenWaiterListFails()
    {
        // Arrange
        _waiterListRepo.ContainsAsync("user@test.com")
            .Throws(new Exception("Waiter list unavailable"));

        // Act
        var act = () => _sut.SignUpAsync("user@test.com", "Pass123!", "John", "Doe");

        // Assert
        await act.Should().ThrowAsync<Exception>();

        await _cognito.DidNotReceive().SignUpAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task SignIn_ShouldReturnCorrectUsernameAndRole_FromJwtClaims()
    {
        var fakeToken = GenerateFakeJwt("John", "Doe", "CUSTOMER");

        _cognito.SignInAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((fakeToken, "refresh-token"));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Username.Should().Be("John Doe");
        result.Role.Should().Be("CUSTOMER");
        result.IdToken.Should().Be(fakeToken);
        result.RefreshToken.Should().Be("refresh-token");
    }



    [Fact]
    public async Task SignIn_ShouldReturnWaiterRole_WhenTokenContainsWaiterRole()
    {
        var fakeToken = GenerateFakeJwt("Bob", "Smith", "WAITER");
        _cognito.SignInAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((fakeToken, "refresh-token"));

        var result = await _sut.SignInAsync("waiter@test.com", "Pass123!");

        result.Role.Should().Be("WAITER");
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

        _cognito.SignInAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((fakeToken, "refresh-token"));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Role.Should().Be("CUSTOMER");
    }

    [Fact]
    public async Task SignIn_ShouldReturnEmptyUsername_WhenNameClaimsMissing()
    {
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim("custom:role", "CUSTOMER")
        });
        var fakeToken = new JwtSecurityTokenHandler().WriteToken(token);

        _cognito.SignInAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((fakeToken, "refresh-token"));

        var result = await _sut.SignInAsync("user@test.com", "Pass123!");

        result.Username.Should().BeEmpty();
    }

    [Fact]
    public async Task SignIn_ShouldPropagate_WhenCognitoThrows()
    {
        _cognito.SignInAsync(Arg.Any<string>(), Arg.Any<string>())
            .Throws(new InvalidCredentialsException());

        await _sut.Invoking(s => s.SignInAsync("user@test.com", "wrong"))
            .Should().ThrowAsync<InvalidCredentialsException>();
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

