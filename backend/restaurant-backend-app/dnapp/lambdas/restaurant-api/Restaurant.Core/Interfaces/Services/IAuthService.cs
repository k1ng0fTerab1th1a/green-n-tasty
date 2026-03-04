<<<<<<< HEAD:backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Core/Interfaces/Services/IAuthService.cs
﻿using Restaurant.Core.SharedModels;
=======
﻿using System.Security.Claims;
using Restaurant.Core.Models;
>>>>>>> 7a195bb (expose IAuthService.IsWaiter for role checks):backend/restaurant-backend-app/dnapp/lambdas/restaurant-api/Restaurant.Core/Interfaces/IAuthService.cs

namespace Restaurant.Core.Interfaces.Services;

public interface IAuthService
{
    Task SignUpAsync(string email, string password, string firstName, string lastName);
    Task<AuthResult> SignInAsync(string email, string password);

    bool IsWaiter(ClaimsPrincipal user);
}