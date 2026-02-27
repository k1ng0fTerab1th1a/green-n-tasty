using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Configuration;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces;

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

    public async Task<string> SignUpAsync(string email, string password, string firstName, string lastName, string role = "CUSTOMER")
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
            });

            await _client.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            });

            return response.UserSub;
        }
        catch (UsernameExistsException)
        {
            throw new UserAlreadyExistsException();
        }
    }

    public async Task<(string AccessToken, string RefreshToken)> SignInAsync(string email, string password)
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

            var response = await _client.InitiateAuthAsync(request);

            return (response.AuthenticationResult.AccessToken, response.AuthenticationResult.RefreshToken);
        }
        catch (NotAuthorizedException)
        {
            throw new InvalidCredentialsException();
        }
        catch (UserNotFoundException)
        {
            throw new InvalidCredentialsException();
        }
    }

    public async Task DeleteUserAsync(string email)
    {
        try
        {
            var request = new AdminDeleteUserRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            };

            await _client.AdminDeleteUserAsync(request);
        }
        catch (UserNotFoundException)
        {
            throw new UserNotFoundException($"User with email {email} not found.");
        }
    }

    public async Task<string> RefreshTokenAsync(string refreshToken)
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

        var response = await _client.InitiateAuthAsync(request);

        return response.AuthenticationResult.AccessToken;
    }

    public async Task SignOutAsync(string refreshToken)
    {
        var request = new RevokeTokenRequest
        {
            Token = refreshToken,
            ClientId = _clientId
        };

        try
        {
            await _client.RevokeTokenAsync(request);
        }
        catch (UnsupportedOperationException)
        {
            throw new AuthException("Token revocation is not supported or enabled.");
        }
        catch (Exception)
        {
            throw new AuthException("Failed to log out due to an internal authentication error.");
        }
    }
}