using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface IUserService
{
    Task<Result> UpdateEmailAsync(string newEmail, string accessToken, CancellationToken ct);
    Task<Result> VerifyEmailChangeAsync(string userId, string newEmail, string accessToken, string code, CancellationToken ct);
    Task<Result> UpdateUserNameAsync(string userId, string firstName, string lastName, CancellationToken ct);
    Task<Result> ChangePasswordAsync(string accessToken, string currentPassword, string newPassword, CancellationToken ct);
    Task<Result<string>> UpdateAvatarAsync(string userId, FileUploadDto file, CancellationToken ct);
    Task<Result<User>> GetMeAsync(string userId, CancellationToken ct);
}
