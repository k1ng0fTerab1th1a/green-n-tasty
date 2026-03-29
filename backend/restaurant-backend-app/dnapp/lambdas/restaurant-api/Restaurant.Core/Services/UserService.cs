using FluentResults;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Core.Services;

public class UserService : IUserService
{
    private readonly ICognitoService _cognitoService;
    private readonly IUserRepository _userRepository;

    public UserService(ICognitoService cognitoService, IUserRepository userRepository)
    {
        _cognitoService = cognitoService;
        _userRepository = userRepository;
    }

    public async Task<Result> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        var cognitoResult = await _cognitoService.UpdateUserEmailAsync(userId, newEmail, ct);
        if (cognitoResult.IsFailed)
            return cognitoResult;

        await _userRepository.UpdateEmailAsync(userId, newEmail, ct);

        return Result.Ok();
    }
}
