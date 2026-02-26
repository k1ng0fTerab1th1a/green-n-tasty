using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Restaurant.Core.Exceptions;
using Restaurant.Core.Interfaces;

namespace Restaurant.Infrastructure.Services;

public class CognitoService : ICognitoService
{
    private readonly AmazonCognitoIdentityProviderClient _client;
    private readonly string _userPoolId;
    private readonly string _clientId;

    public CognitoService()
    {
        _client = new AmazonCognitoIdentityProviderClient();
        _userPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? throw new ArgumentNullException("COGNITO_USER_POOL_ID is missing");
        _clientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? throw new ArgumentNullException("COGNITO_CLIENT_ID is missing");
    }

    public string GetUserPoolId() => _userPoolId;

    public async Task SignUpAsync(string email, string password, string firstName, string lastName)
    {
        try
        {
            await _client.SignUpAsync(new SignUpRequest
            {
                ClientId = _clientId,
                Username = email,
                Password = password,
                UserAttributes = new List<AttributeType>
                {
                    new() { Name = "email", Value = email },
                    new() { Name = "given_name", Value = firstName },
                    new() { Name = "family_name", Value = lastName }
                }
            });

            await _client.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            });
        }
        catch (UsernameExistsException)
        {
            throw new UserAlreadyExistsException();
        }
    }

    public async Task<string> SignInAsync(string email, string password)
    {
        try
        {
            var response = await _client.InitiateAuthAsync(new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _clientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            });

            return response.AuthenticationResult.IdToken;
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
}