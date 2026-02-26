using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces;

public interface IAuthService
{
    Task SignUpAsync(string email, string password, string firstName, string lastName);
    Task<AuthResult> SignInAsync(string email, string password);
}