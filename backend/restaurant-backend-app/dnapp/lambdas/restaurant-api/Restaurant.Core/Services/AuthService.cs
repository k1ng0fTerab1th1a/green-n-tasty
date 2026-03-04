using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class AuthService : IAuthService
{
    private readonly ICognitoService _cognitoService;
    private readonly IUserRepository _userRepository;
    private readonly IWaiterListRepository _waiterListRepository;

    public AuthService(ICognitoService cognitoService, IUserRepository userRepository, IWaiterListRepository waiterListRepository)
    {
        _cognitoService = cognitoService;
        _userRepository = userRepository;
        _waiterListRepository = waiterListRepository;
    }

    public async Task SignUpAsync(string email, string password, string firstName, string lastName)
    {
        var role = "CUSTOMER";
        if(await IsWaiter(email)) role = "WAITER";

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
        var (idToken, refreshToken) = await _cognitoService.SignInAsync(email, password);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(idToken);

        var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "";
        var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "";

        var username = $"{firstName} {lastName}".Trim();

        var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "custom:role")?.Value ?? "CUSTOMER";

        return new AuthResult(idToken, refreshToken, username, role);
    }

    public bool IsWaiter(ClaimsPrincipal user)
    {
        var role = user?.FindFirst("custom:role")?.Value
                   ?? user?.FindFirst(ClaimTypes.Role)?.Value;

        return string.Equals(role, "WAITER", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsWaiter(string email)
    {
        return await _waiterListRepository.ContainsAsync(email);
    }
}