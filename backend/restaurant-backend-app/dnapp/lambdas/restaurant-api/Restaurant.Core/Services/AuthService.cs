using FluentResults;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.SharedModels;
using System.IdentityModel.Tokens.Jwt;

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

    public async Task<Result> SignUpAsync(string email, string password, string firstName, string lastName)
    {
        var role = "CUSTOMER";
        bool isWaiter = await IsWaiter(email);
        if (isWaiter) role = "WAITER";

        var signUpResult = await _cognitoService.SignUpAsync(email, password, firstName, lastName, role);
        if (signUpResult.IsFailed)
            return signUpResult.ToResult();

        try
        {
            var user = new User
            {
                UserId = signUpResult.Value,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };
            if (isWaiter) user.WaiterFlag = "1";

            await _userRepository.CreateAsync(user);
        }
        catch (Exception)
        {
            await _cognitoService.DeleteUserAsync(email);
            throw;
        }

        return Result.Ok();
    }

    public async Task<Result<AuthResult>> SignInAsync(string email, string password)
    {
        var signInResult = await _cognitoService.SignInAsync(email, password);
        if (signInResult.IsFailed)
            return Result.Fail<AuthResult>(signInResult.Errors);

        var (idToken, refreshToken) = signInResult.Value;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(idToken);

        var firstName = jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "";
        var lastName = jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "";
        var username = $"{firstName} {lastName}".Trim();
        var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "custom:role")?.Value ?? "CUSTOMER";

        return new AuthResult(idToken, refreshToken, username, role);
    }

    private async Task<bool> IsWaiter(string email)
    {
        return await _waiterListRepository.ContainsAsync(email);
    }
}