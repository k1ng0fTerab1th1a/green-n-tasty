using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.SharedModels;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICognitoService _cognitoService;

    public AuthController(IAuthService authService, ICognitoService cognitoService)
    {
        _authService = authService;
        _cognitoService = cognitoService;
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] Contracts.Requests.SignUpRequest request)
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

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _cognitoService.RefreshTokenAsync(request.RefreshToken);
        return ApiResponse<string>.Success(StatusCodes.Status200OK, result, "Token refreshed successfully");
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut([FromBody] SignOutRequest request)
    {
        await _cognitoService.SignOutAsync(request.RefreshToken);

        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Logged out successfully");
    }
}