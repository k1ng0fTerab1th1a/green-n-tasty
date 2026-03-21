using FluentResults;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Interfaces.Services;

public interface IAuthService
{
    Task<Result> SignUpAsync(string email, string password, string firstName, string lastName);
    Task<Result<AuthResult>> SignInAsync(string email, string password);
}