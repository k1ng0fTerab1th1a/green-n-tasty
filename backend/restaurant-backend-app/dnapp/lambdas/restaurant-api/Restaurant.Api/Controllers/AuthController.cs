using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ApiResponse<object>> SignUp([FromBody] SignUpRequest request)
    {
        var result = await _authService.SignUpAsync(request.Email, request.Password, request.FirstName, request.LastName);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return ApiResponse<object>.Success(StatusCodes.Status201Created, null, "User registered successfully");
    }

    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<AuthResult>> SignIn([FromBody] SignInRequest request)
    {
        var result = await _authService.SignInAsync(request.Email, request.Password);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<AuthResult>();

        return ApiResponse<AuthResult>.Success(StatusCodes.Status200OK, result.Value, "Authentication successful");
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<string>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var token = await _cognitoService.RefreshTokenAsync(request.RefreshToken);
        return ApiResponse<string>.Success(StatusCodes.Status200OK, token, "Token refreshed successfully");
    }

    [HttpPost("sign-out")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> SignOut([FromBody] SignOutRequest request)
    {
        await _cognitoService.SignOutAsync(request.RefreshToken);
        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Logged out successfully");
    }
}