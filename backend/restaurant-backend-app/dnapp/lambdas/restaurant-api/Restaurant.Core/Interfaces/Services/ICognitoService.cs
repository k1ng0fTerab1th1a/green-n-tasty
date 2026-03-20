using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface ICognitoService
{
    string GetUserPoolId();
    Task<Result<string>> SignUpAsync(string email, string password, string firstName, string lastName, string role = "CUSTOMER");
    Task<Result<(string IdToken, string RefreshToken)>> SignInAsync(string email, string password);
    Task DeleteUserAsync(string email);
    Task<string> RefreshTokenAsync(string refreshToken);
    Task SignOutAsync(string refreshToken);
}
