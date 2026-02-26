using System.IdentityModel.Tokens.Jwt;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class AuthService : IAuthService
{
    private readonly ICognitoService _cognitoService;

    public AuthService(ICognitoService cognitoService)
    {
        _cognitoService = cognitoService;
    }

    public async Task SignUpAsync(string email, string password, string firstName, string lastName)
    {
        await _cognitoService.SignUpAsync(email, password, firstName, lastName);
    }

    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        var token = await _cognitoService.SignInAsync(email, password);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "";
        var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "";

        var username = $"{firstName} {lastName}".Trim();

        if (string.IsNullOrEmpty(username))
        {
            username = email.Split('@')[0];
        }

        var role = "CLIENT";

        return new AuthResult(token, username, role);
    }
}