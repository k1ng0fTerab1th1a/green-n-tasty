using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface ICognitoService
{
    string GetUserPoolId();
    Task<Result<string>> SignUpAsync(string email, string password, string firstName, string lastName, string role, CancellationToken ct);
    Task<Result<(string IdToken, string AccessToken, string RefreshToken)>> SignInAsync(string email, string password, CancellationToken ct);
    Task<Result> DeleteUserAsync(string email, CancellationToken ct);
    Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<Result> SignOutAsync(string refreshToken, CancellationToken ct);
    Task<Result> UpdatePasswordAsync(string email, string newPassword, CancellationToken ct = default);
    Task<Result> UpdateUserEmailAsync(string accessToken, string newEmail, CancellationToken ct);
    Task<Result> VerifyEmailChangeAsync(string accessToken, string code, CancellationToken ct);
}
