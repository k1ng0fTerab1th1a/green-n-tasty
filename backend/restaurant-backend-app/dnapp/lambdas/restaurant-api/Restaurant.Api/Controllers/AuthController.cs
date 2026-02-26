using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.DTOs;
using Restaurant.Api.Models;
using Restaurant.Core.Interfaces;
using Restaurant.Core.Models;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        await _authService.SignUpAsync(request.Email, request.Password, request.FirstName, request.LastName);

        return ApiResponse<object>.Success(StatusCodes.Status201Created, null, "User registered successfully");
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        var result = await _authService.SignInAsync(request.Email, request.Password);

        return ApiResponse<AuthResult>.Success(StatusCodes.Status200OK, result, "Authentication successful");
    }
}