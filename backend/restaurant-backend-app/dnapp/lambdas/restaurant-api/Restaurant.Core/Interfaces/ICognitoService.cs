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
        Task SignUpAsync(string email, string password, string firstName, string lastName);
        Task<string> SignInAsync(string email, string password);
    }
}
