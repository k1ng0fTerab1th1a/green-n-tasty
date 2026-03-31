using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Authorize]
[Route("feedbacks")]
public class FeedbacksController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbacksController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }
    
    [HttpPost("authorised")]
    public async Task<ApiResponse<object>> CreateFeedbackAuthorised([FromBody] CreateFeedbackDTO req, CancellationToken
        ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<object>.Fail(401, "User should be authorised to leave feedback");

        var result = await _feedbackService.SaveAuthorisedFeedback(req, userId, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();
        return ApiResponse<object>.Success(200, null!);
    }

    [AllowAnonymous]
    [HttpPost("visitor")]
    public async Task<ApiResponse<object>> CreateFeedbackVisitor([FromBody] CreateFeedbackDTO req, [FromQuery] string
        secretCode, CancellationToken ct)
    {
        var result = await _feedbackService.SaveVisitorFeedback(req, secretCode, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();
        return ApiResponse<object>.Success(200, null!); 
    }

    [HttpGet("feedback-short-data")]
    public async Task<ApiResponse<WaiterLocationFeedbackDTO>> GetShortFeedbackData([FromQuery] string reservationId,
        CancellationToken ct)
    {
        var res = await _feedbackService.GetWaiterLocationFeedbackDtoAsync(reservationId, false, ct);
        if (res.IsSuccess)
            return ApiResponse<WaiterLocationFeedbackDTO>.Success(200, res.Value);
        return res.Errors[0].ToApiResponse<WaiterLocationFeedbackDTO>();
    }

    [HttpGet("feedback-short-update-data")]
    public async Task<ApiResponse<WaiterLocationFeedbackDTO>> GetUpdateFeedbackData([FromQuery] string reservationId,
        CancellationToken ct)
    {
        var res = await _feedbackService.GetWaiterLocationFeedbackDtoAsync(reservationId, true, ct);
        if (res.IsSuccess)
            return ApiResponse<WaiterLocationFeedbackDTO>.Success(200, res.Value);
        return res.Errors[0].ToApiResponse<WaiterLocationFeedbackDTO>();
    }

    [HttpPut("update-feedback")]
    public async Task<ApiResponse<object>> UpdateFeedback([FromBody] UpdateFeedbackRequest request,
        CancellationToken ct)
    {
        var res = await _feedbackService.UpdateFeedback(
            request.FeedbackId, request.Comment, request.Rating, request.FeedbackType, ct);
        
        if (res.IsSuccess)
            return ApiResponse<object>.Success(200, null);
        return res.Errors[0].ToApiResponse<object>();
    }
}