using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface IUserService
{
    Task<Result> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct);
}
