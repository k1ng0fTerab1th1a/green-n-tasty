using System;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class AuthService : IAuthService
{
    private readonly ICognitoService _cognitoService;
    private readonly IUserRepository _userRepository;

    public AuthService(ICognitoService cognitoService, IUserRepository userRepository)
    {
        _cognitoService = cognitoService;
        _userRepository = userRepository;
    }

    public async Task SignUpAsync(string email, string password, string firstName, string lastName)
    {
        var role = "CUSTOMER";
        if(IsWaiter()) role = "WAITER";

        var userId = await _cognitoService.SignUpAsync(email, password, firstName, lastName, role);

        try
        {
            var user = new User
            {
                UserId = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };

            await _userRepository.CreateAsync(user);
        }
        catch (Exception)
        {
            await _cognitoService.DeleteUserAsync(email);
            throw;
        }
    }

    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        var (accessToken, refreshToken) = await _cognitoService.SignInAsync(email, password);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);

        var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "";
        var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "";

        var username = $"{firstName} {lastName}".Trim();

        var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "custom:role")?.Value ?? "CUSTOMER";

        return new AuthResult(accessToken, refreshToken, username, role);
    }

    private bool IsWaiter()
    {
        return false;
    }
}