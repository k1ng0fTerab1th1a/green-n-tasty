using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Interfaces
{
    public interface ICognitoService
    {
        string GetUserPoolId();
        Task<string> SignUpAsync(string email, string password, string firstName, string lastName, string role = "CUSTOMER");
        Task<(string IdToken, string RefreshToken)> SignInAsync(string email, string password);
        Task DeleteUserAsync(string email);
        Task<string> RefreshTokenAsync(string refreshToken);

        Task SignOutAsync(string refreshToken);
    }
}
