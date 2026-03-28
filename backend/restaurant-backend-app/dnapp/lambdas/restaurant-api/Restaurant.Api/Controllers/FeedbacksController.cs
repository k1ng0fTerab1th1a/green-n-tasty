using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        var validation = ValidateFeedbackData(req);
        if (!validation.IsSuccess)
            return validation;
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
        var validation = ValidateFeedbackData(req);
        if (!validation.IsSuccess)
            return validation;
        var result = await _feedbackService.SaveVisitorFeedback(req, secretCode, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();
        return ApiResponse<object>.Success(200, null!); 
    }

    [HttpGet("feedback-short-data")]
    public async Task<ApiResponse<WaiterLocationFeedbackDTO>> GetOverallFeedbackData([FromQuery] string reservationId,
        CancellationToken ct = default)
    {
        var res = await _feedbackService.GetWaiterLocationFeedbackDTOAsync(reservationId, ct);
        if (res.IsSuccess)
            return ApiResponse<WaiterLocationFeedbackDTO>.Success(200, res.Value);
        return res.Errors[0].ToApiResponse<WaiterLocationFeedbackDTO>();
    }

    private static ApiResponse<object> ValidateFeedbackData(CreateFeedbackDTO dto)
    {
        if (dto.CuisineRating == null && dto.ServiceRating == null)
            return ApiResponse<object>.Fail(400, "Either one feedback or another should be present");

        return ValidateRatingAndComment(dto.CuisineRating, dto.CuisineComment)
               ?? ValidateRatingAndComment(dto.ServiceRating, dto.ServiceComment)
               ?? ApiResponse<object>.Success(200, null!);
    }

    private static ApiResponse<object>? ValidateRatingAndComment(int? rating, string? comment)
    {
        if (rating == null)
            return null;

        if (rating < 1 || rating > 5)
            return ApiResponse<object>.Fail(400, "Rating should be within 1 and 5 stars");

        if (!string.IsNullOrEmpty(comment) && comment.Length > 300)
            return ApiResponse<object>.Fail(400, "Comment length should not be longer than 300 characters");

        return null;
    }
}