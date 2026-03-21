using Amazon;
using Amazon.CognitoIdentityProvider;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Restaurant.Core.Errors;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Infrastructure.IntegrationTests;

public sealed class CognitoServiceLiveIntegrationTests
{
    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task AuthFlow_ShouldWork()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);

        var userDeleted = false;
        try
        {
            var signUpResult = await sut.SignUpAsync(user.Email, user.Password, "Live", "Test", role: "CUSTOMER");
            signUpResult.IsSuccess.Should().BeTrue();
            signUpResult.Value.Should().NotBeNullOrWhiteSpace();

            var signInResult = await sut.SignInAsync(user.Email, user.Password);
            signInResult.IsSuccess.Should().BeTrue();
            signInResult.Value.IdToken.Should().NotBeNullOrWhiteSpace();
            signInResult.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

            var accessToken = await sut.RefreshTokenAsync(signInResult.Value.RefreshToken);
            accessToken.Should().NotBeNullOrWhiteSpace();

            await sut.SignOutAsync(signInResult.Value.RefreshToken);

            await sut.DeleteUserAsync(user.Email);
            userDeleted = true;
        }
        finally
        {
            if (!userDeleted)
            {
                await SafeDeleteAsync(sut, user.Email);
            }
        }
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task SignUp_ShouldFail_WhenEmailAlreadyExists()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);

        await sut.SignUpAsync(user.Email, user.Password, "Live", "Duplicate", role: "CUSTOMER");

        try
        {
            var result = await sut.SignUpAsync(user.Email, user.Password, "Live", "Duplicate", role: "CUSTOMER");
            result.IsFailed.Should().BeTrue();
            result.Errors[0].Should().Be(AuthErrors.UserAlreadyExists);
        }
        finally
        {
            await SafeDeleteAsync(sut, user.Email);
        }
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task SignIn_ShouldFail_WhenPasswordWrong()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);

        await sut.SignUpAsync(user.Email, user.Password, "Live", "WrongPassword", role: "CUSTOMER");

        try
        {
            var result = await sut.SignInAsync(user.Email, "Wrong123!Password");
            result.IsFailed.Should().BeTrue();
            result.Errors[0].Should().Be(AuthErrors.InvalidCredentials);
        }
        finally
        {
            await SafeDeleteAsync(sut, user.Email);
        }
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task SignIn_ShouldFail_WhenUserNotFound()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var missingEmail = $"missing-live-{Guid.NewGuid():N}@{settings.EmailDomain}";

        var result = await sut.SignInAsync(missingEmail, settings.Password);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.InvalidCredentials);
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task DeleteUser_ShouldRemoveUser()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);

        await sut.SignUpAsync(user.Email, user.Password, "Live", "Delete", role: "CUSTOMER");

        var deleteResult = await sut.DeleteUserAsync(user.Email);
        deleteResult.IsSuccess.Should().BeTrue();

        var result = await sut.DeleteUserAsync(user.Email);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.UserNotFound);
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task DeleteUser_ShouldFail_WhenUserMissing()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var missingEmail = $"missing-live-{Guid.NewGuid():N}@{settings.EmailDomain}";

        await sut.Invoking(x => x.DeleteUserAsync(missingEmail))
            .Should().ThrowAsync<Amazon.CognitoIdentityProvider.Model.UserNotFoundException>();
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task RefreshToken_ShouldFail_WhenTokenInvalid()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);

        await sut.Invoking(x => x.RefreshTokenAsync("not-a-valid-refresh-token"))
            .Should().ThrowAsync<Exception>();
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task SignOut_ShouldFail_WhenTokenInvalid()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);

        var result = await sut.SignOutAsync("not-a-valid-refresh-token");
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.SignOutFailed);
    }

    private static LiveCognitoSettings GetRequiredSettings()
    {
        var region = Environment.GetEnvironmentVariable("SYSTEM_AWS_REGION");
        var userPoolId = Environment.GetEnvironmentVariable("SYSTEM_COGNITO_USER_POOL_ID");
        var clientId = Environment.GetEnvironmentVariable("SYSTEM_COGNITO_CLIENT_ID");
        var password = "Pass12345!A";
        var emailDomain = "example.com";
        var environmentName = "dev";

        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(userPoolId) || string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Missing env vars for live Cognito tests. Required: SYSTEM_AWS_REGION, SYSTEM_COGNITO_USER_POOL_ID, SYSTEM_COGNITO_CLIENT_ID");
        }

        return new LiveCognitoSettings(region, userPoolId, clientId, password, emailDomain, environmentName);
    }

    private static CognitoService CreateSut(LiveCognitoSettings settings)
    {
        var client = new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(settings.Region));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cognito:UserPoolId"] = settings.UserPoolId,
                ["Cognito:ClientId"] = settings.ClientId
            })
            .Build();

        return new CognitoService(client, config);
    }

    private static TestUser CreateTestUser(LiveCognitoSettings settings)
    {
        var email = $"cognito-live-{Guid.NewGuid():N}@{settings.EmailDomain}";
        return new TestUser(email, settings.Password);
    }

    private static async Task SafeDeleteAsync(CognitoService sut, string email)
    {
        try
        {
            await sut.DeleteUserAsync(email);
        }
        catch (Amazon.CognitoIdentityProvider.Model.UserNotFoundException)
        {
            // The test user is already removed.
        }
    }

    private sealed record LiveCognitoSettings(
        string Region,
        string UserPoolId,
        string ClientId,
        string Password,
        string EmailDomain,
        string EnvironmentName);

    private sealed record TestUser(string Email, string Password);
}
