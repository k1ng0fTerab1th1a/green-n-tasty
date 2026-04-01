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

            await AdminConfirmSignUpForTestAsync(settings, user.Email);

            var signInResult = await sut.SignInAsync(user.Email, user.Password);
            signInResult.IsSuccess.Should().BeTrue();
            signInResult.Value.IdToken.Should().NotBeNullOrWhiteSpace();
            signInResult.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

            var refreshResult = await sut.RefreshTokenAsync(signInResult.Value.RefreshToken);
            refreshResult.IsSuccess.Should().BeTrue();
            refreshResult.Value.Should().NotBeNullOrWhiteSpace();

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
        await AdminConfirmSignUpForTestAsync(settings, user.Email);

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
        await AdminConfirmSignUpForTestAsync(settings, user.Email);

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

        var result = await sut.DeleteUserAsync(missingEmail);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.UserNotFound);
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task RefreshToken_ShouldFail_WhenTokenInvalid()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);

        var result = await sut.RefreshTokenAsync("not-a-valid-refresh-token");
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.RefreshTokenFailed);
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task UpdateEmail_ShouldSucceed_AndAllowLoginWithNewEmail()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);
        var newEmail = $"cognito-live-updated-{Guid.NewGuid():N}@{settings.EmailDomain}";

        await sut.SignUpAsync(user.Email, user.Password, "Live", "UpdateEmail", role: "CUSTOMER");
        await AdminConfirmSignUpForTestAsync(settings, user.Email);

        try
        {
            var signInResult = await sut.SignInAsync(user.Email, user.Password);
            signInResult.IsSuccess.Should().BeTrue();
            var userId = signInResult.Value.IdToken;

            var updateResult = await sut.UpdateUserEmailAsync(user.Email, newEmail);
            updateResult.IsSuccess.Should().BeTrue();

            var loginWithNew = await sut.SignInAsync(newEmail, user.Password);
            loginWithNew.IsSuccess.Should().BeTrue();
            loginWithNew.Value.IdToken.Should().NotBeNullOrWhiteSpace();

            var loginWithOld = await sut.SignInAsync(user.Email, user.Password);
            loginWithOld.IsFailed.Should().BeTrue();
            loginWithOld.Errors[0].Should().Be(AuthErrors.InvalidCredentials);
        }
        finally
        {
            await SafeDeleteAsync(sut, newEmail);
        }
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task UpdateEmail_ShouldFail_WhenUserNotFound()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var missingEmail = $"missing-live-{Guid.NewGuid():N}@{settings.EmailDomain}";
        var newEmail = $"cognito-live-updated-{Guid.NewGuid():N}@{settings.EmailDomain}";

        var result = await sut.UpdateUserEmailAsync(missingEmail, newEmail);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(AuthErrors.UserNotFound);
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task UpdateEmail_ShouldFail_WhenNewEmailAlreadyTaken()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user1 = CreateTestUser(settings);
        var user2 = CreateTestUser(settings);

        await sut.SignUpAsync(user1.Email, user1.Password, "Live", "EmailConflict1", role: "CUSTOMER");
        await sut.SignUpAsync(user2.Email, user2.Password, "Live", "EmailConflict2", role: "CUSTOMER");

        try
        {
            var result = await sut.UpdateUserEmailAsync(user1.Email, user2.Email);
            result.IsFailed.Should().BeTrue();
            result.Errors[0].Should().Be(AuthErrors.UserAlreadyExists);
        }
        finally
        {
            await SafeDeleteAsync(sut, user1.Email);
            await SafeDeleteAsync(sut, user2.Email);
        }
    }

    [LiveCognitoFact]
    [Trait("Category", "LiveCognito")]
    public async Task SignIn_ShouldFail_WhenUserNotConfirmed()
    {
        var settings = GetRequiredSettings();
        var sut = CreateSut(settings);
        var user = CreateTestUser(settings);

        await sut.SignUpAsync(user.Email, user.Password, "Live", "Unconfirmed", role: "CUSTOMER");

        try
        {
            var result = await sut.SignInAsync(user.Email, user.Password);
            result.IsFailed.Should().BeTrue();
            result.Errors[0].Should().Be(AuthErrors.EmailNotVerified);
        }
        finally
        {
            await SafeDeleteAsync(sut, user.Email);
        }
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

    private static async Task AdminConfirmSignUpForTestAsync(LiveCognitoSettings settings, string email)
    {
        using var client = new AmazonCognitoIdentityProviderClient(
            Amazon.RegionEndpoint.GetBySystemName(settings.Region));
        await client.AdminConfirmSignUpAsync(
            new Amazon.CognitoIdentityProvider.Model.AdminConfirmSignUpRequest
            {
                UserPoolId = settings.UserPoolId,
                Username = email
            });
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
