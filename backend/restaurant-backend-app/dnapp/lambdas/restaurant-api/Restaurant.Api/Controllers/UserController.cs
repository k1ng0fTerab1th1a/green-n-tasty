using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
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
    public async Task<ApiResponse<object>> UpdateUsername([FromBody] UpdateUsernameRequest request, CancellationToken
        ct)
    {
        var userId = User.GetUserId();

        var result = await _userService.UpdateUserNameAsync(userId, request.FirstName, request.LastName, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();
        
        return ApiResponse<object>.Success(StatusCodes.Status200OK, null, "Username updated successfully");
    }
}
