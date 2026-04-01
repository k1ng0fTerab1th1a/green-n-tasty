using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Infrastructure.Services;

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _client;
    private readonly string _userPoolId;
    private readonly string _clientId;

    public CognitoService(IAmazonCognitoIdentityProvider client, IConfiguration config)
    {
        _client = client;

        _userPoolId = config["Cognito:UserPoolId"] ?? config["COGNITO_USER_POOL_ID"]
            ?? throw new ArgumentNullException("UserPoolId is missing");

        _clientId = config["Cognito:ClientId"] ?? config["COGNITO_CLIENT_ID"]
            ?? throw new ArgumentNullException("ClientId is missing");
    }

    public string GetUserPoolId() => _userPoolId;

    public async Task<Result<string>> SignUpAsync(string email, string password, string firstName, string lastName, string role = "CUSTOMER", CancellationToken ct = default)
    {
        try
        {
            var response = await _client.SignUpAsync(new SignUpRequest
            {
                ClientId = _clientId,
                Username = email,
                Password = password,
                UserAttributes = new List<AttributeType>
                {
                    new() { Name = "email", Value = email },
                    new() { Name = "given_name", Value = firstName },
                    new() { Name = "family_name", Value = lastName },
                    new() { Name = "custom:role", Value = role }
                }
            }, ct);

            return response.UserSub;
        }
        catch (UsernameExistsException)
        {
            return AuthErrors.UserAlreadyExists;
        }
    }

    public async Task<Result<(string IdToken, string RefreshToken)>> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _clientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var response = await _client.InitiateAuthAsync(request, ct);

            return (response.AuthenticationResult.IdToken, response.AuthenticationResult.RefreshToken);
        }
        catch (UserNotConfirmedException)
        {
            return AuthErrors.EmailNotVerified;
        }
        catch (NotAuthorizedException)
        {
            return AuthErrors.InvalidCredentials;
        }
        catch (UserNotFoundException)
        {
            return AuthErrors.InvalidCredentials;
        }
    }

    public async Task<Result> DeleteUserAsync(string email, CancellationToken ct = default)
    {
        try
        {
            var request = new AdminDeleteUserRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            };

            await _client.AdminDeleteUserAsync(request, ct);
            return Result.Ok();
        }
        catch (UserNotFoundException)
        {
            return AuthErrors.UserNotFound;
        }
    }

    public async Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                ClientId = _clientId,
                AuthParameters = new Dictionary<string, string>
            {
                { "REFRESH_TOKEN", refreshToken }
            }
            };

            var response = await _client.InitiateAuthAsync(request, ct);

            return response.AuthenticationResult.AccessToken;
        }
        catch (AmazonCognitoIdentityProviderException)
        {
            return AuthErrors.RefreshTokenFailed;
        }
    }

    public async Task<Result> SignOutAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = new RevokeTokenRequest
        {
            Token = refreshToken,
            ClientId = _clientId
        };

        try
        {
            await _client.RevokeTokenAsync(request, ct);
            return Result.Ok();
        }
        catch (UnsupportedOperationException)
        {
            throw;
        }
        catch (AmazonCognitoIdentityProviderException)
        {
            return AuthErrors.SignOutFailed;
        }
    }

    public async Task<Result> UpdateUserEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        try
        {
            await _client.AdminUpdateUserAttributesAsync(new AdminUpdateUserAttributesRequest
            {
                UserPoolId = _userPoolId,
                Username = userId,
                UserAttributes = new List<AttributeType>
                {
                    new() { Name = "email", Value = newEmail },
                    new() { Name = "email_verified", Value = "true" }
                }
            }, ct);

            return Result.Ok();
        }
        catch (UserNotFoundException)
        {
            return AuthErrors.UserNotFound;
        }
        catch (AliasExistsException)
        {
            return AuthErrors.UserAlreadyExists;
        }
    }

    public async Task<Result> UpdatePasswordAsync(string email, string newPassword, CancellationToken ct = default)
    {
        try
        {
            var request = new AdminSetUserPasswordRequest
            {
                UserPoolId = _userPoolId,
                Username = email,
                Password = newPassword,
                Permanent = true
            };

            await _client.AdminSetUserPasswordAsync(request, ct);

            return Result.Ok();
        }
        catch (UserNotFoundException)
        {
            return AuthErrors.UserNotFound;
        }
        catch (InvalidPasswordException)
        {
            return AuthErrors.InvalidPassword;
        }
        catch (Exception)
        {
            return Result.Fail("Password update failed");
        }
    }
}