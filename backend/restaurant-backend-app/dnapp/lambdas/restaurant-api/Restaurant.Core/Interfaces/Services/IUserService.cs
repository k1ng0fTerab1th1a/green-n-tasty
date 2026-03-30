using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface IUserService
{
    Task<Result> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct);
    Task<Result> UpdateUserNameAsync(string userId, string firstName, string lastName, CancellationToken ct);
}
