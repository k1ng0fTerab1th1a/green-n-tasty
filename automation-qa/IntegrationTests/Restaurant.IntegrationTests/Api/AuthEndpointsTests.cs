using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.Errors;
using Restaurant.Core.SharedModels;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SignUp_ShouldReturn201_AndCallAuthService()
    {
        _factory.AuthService.Reset();

        var res = await _client.PostAsync("/auth/sign-up", Json(new
        {
            email = "user@test.com",
            password = "Pass123!",
            firstName = "John",
            lastName = "Doe"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("User registered successfully");

        _factory.AuthService.LastSignUpEmail.Should().Be("user@test.com");
        _factory.AuthService.LastSignUpFirstName.Should().Be("John");
        _factory.AuthService.LastSignUpLastName.Should().Be("Doe");
    }

    [Fact]
    public async Task SignUp_WhenBodyInvalid_ShouldReturn400_AndNotCallService()
    {
        _factory.AuthService.Reset();

        var res = await _client.PostAsync("/auth/sign-up", Json(new
        {
            email = "bad-email",
            password = "short",
            firstName = "A",
            lastName = "B"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.AuthService.LastSignUpEmail.Should().BeNull();
    }

    [Fact]
    public async Task SignUp_WhenUserAlreadyExists_ShouldReturn409()
    {
        _factory.AuthService.Reset();
        _factory.AuthService.SignUpFailResult = new BusinessError("User with this email already exists!", ErrorType.Conflict);

        var res = await _client.PostAsync("/auth/sign-up", Json(new
        {
            email = "user@test.com",
            password = "Pass123!",
            firstName = "John",
            lastName = "Doe"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task SignIn_ShouldReturn200_WithAuthResult()
    {
        _factory.AuthService.Reset();
        _factory.AuthService.SignInResponse = new AuthResult("id-token-1", "access-1", "refresh-1", "John Doe", "CUSTOMER");

        var res = await _client.PostAsync("/auth/sign-in", Json(new
        {
            email = "user@test.com",
            password = "Pass123!"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("idToken").GetString().Should().Be("id-token-1");
        data.GetPropertyIgnoreCase("accessToken").GetString().Should().Be("access-1");
        data.GetPropertyIgnoreCase("refreshToken").GetString().Should().Be("refresh-1");
        data.GetPropertyIgnoreCase("username").GetString().Should().Be("John Doe");
        data.GetPropertyIgnoreCase("role").GetString().Should().Be("CUSTOMER");

        _factory.AuthService.LastSignInEmail.Should().Be("user@test.com");
    }

    [Fact]
    public async Task SignIn_WhenInvalidCredentials_ShouldReturn401()
    {
        _factory.AuthService.Reset();
        _factory.AuthService.SignInFailResult = new BusinessError("Invalid email or password!", ErrorType.Unauthorized);

        var res = await _client.PostAsync("/auth/sign-in", Json(new
        {
            email = "user@test.com",
            password = "Pass123!"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturn200_AndMappedPayload()
    {
        _factory.CognitoService.Reset();
        _factory.CognitoService.RefreshTokenResponse = "access-2";

        var res = await _client.PostAsync("/auth/refresh-token", Json(new
        {
            refreshToken = "refresh-abc"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data").GetString().Should().Be("access-2");
        _factory.CognitoService.LastRefreshTokenInput.Should().Be("refresh-abc");
    }

    [Fact]
    public async Task RefreshToken_WhenMissingToken_ShouldReturn400()
    {
        _factory.CognitoService.Reset();

        var res = await _client.PostAsync("/auth/refresh-token", Json(new
        {
            refreshToken = ""
        }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.CognitoService.LastRefreshTokenInput.Should().BeNull();
    }

    [Fact]
    public async Task SignOut_ShouldReturn200_AndCallCognitoService()
    {
        _factory.CognitoService.Reset();

        var res = await _client.PostAsync("/auth/sign-out", Json(new
        {
            refreshToken = "refresh-end"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CognitoService.LastSignOutRefreshToken.Should().Be("refresh-end");
    }

    [Fact]
    public async Task SignIn_WhenEmailNotVerified_ShouldReturn403()
    {
        _factory.AuthService.Reset();
        _factory.AuthService.SignInFailResult = AuthErrors.EmailNotVerified;

        var res = await _client.PostAsync("/auth/sign-in", Json(new
        {
            email = "user@test.com",
            password = "Pass123!"
        }));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should()
            .Be(AuthErrors.EmailNotVerified.Message);
    }

    private static StringContent Json(object payload)
    {
        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }
}
