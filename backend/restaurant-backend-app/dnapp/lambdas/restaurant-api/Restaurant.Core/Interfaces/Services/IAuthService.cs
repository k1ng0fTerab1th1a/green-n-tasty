using System.Security.Claims;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Interfaces.Services;

public interface IAuthService
{
    Task SignUpAsync(string email, string password, string firstName, string lastName);
    Task<AuthResult> SignInAsync(string email, string password);
}