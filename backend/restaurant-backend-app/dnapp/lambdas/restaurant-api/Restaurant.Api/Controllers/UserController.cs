using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPut("email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ApiResponse<object>> UpdateEmail([FromBody] UpdateEmailRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var result = await _userService.UpdateEmailAsync(userId!, request.NewEmail, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Email updated successfully");
    }

    [HttpPut("username")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<object>> UpdateUsername([FromBody] UpdateUsernameRequest request, CancellationToken
        ct)
    {
        var userId = User.GetUserId();

        var result = await _userService.UpdateUserNameAsync(userId, request.FirstName, request.LastName, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Username updated successfully");
    }

    [HttpPut("password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<object>> UpdatePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var accessToken = ExtractBearerToken();
        if (string.IsNullOrWhiteSpace(accessToken))
            return AuthErrors.AccessTokenRequired.ToApiResponse<object>();

        var result = await _userService.ChangePasswordAsync(
            accessToken,
            request.CurrentPassword,
            request.NewPassword,
            ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return ApiResponse<object>.Success(
            StatusCodes.Status200OK,
            null,
            "Password updated successfully");
    }

    [HttpPost("avatar")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ApiResponse<string>> UpdateAvatar(IFormFile file, CancellationToken ct)
    {
        var userId = User.GetUserId();

        using var stream = file.OpenReadStream();
        var dto = new FileUploadDto(stream, file.ContentType, file.Length);

        var result = await _userService.UpdateAvatarAsync(userId, dto, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<string>();

        return ApiResponse<string>.Success(StatusCodes.Status200OK, result.Value, "Photo updated successfully");
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return ApiResponse<UserResponse>.Fail(StatusCodes.Status401Unauthorized, "Unauthorized");

        var result = await _userService.GetMeAsync(userId, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<UserResponse>();

        return ApiResponse<UserResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse(), "User retrieved successfully");
    }

    private string? ExtractBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            return null;

        var value = authorizationHeader.ToString();
        const string prefix = "Bearer ";

        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return value[prefix.Length..].Trim();
    }
}
