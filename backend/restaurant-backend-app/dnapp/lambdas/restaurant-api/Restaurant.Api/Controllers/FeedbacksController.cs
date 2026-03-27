using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Api.Controllers;

[ApiController]
[Authorize]
[Route("feedbacks")]
public class FeedbacksController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IFeedbackService _feedbackService;
    private readonly Core.Services.QrCoder _qrCoder;

    public FeedbacksController(IReservationService reservationService, IFeedbackService feedbackService)
    {
        _reservationService = reservationService;
        _feedbackService = feedbackService;
        _qrCoder = new Core.Services.QrCoder();
    }
    
    [HttpPost("authorised")]
    public async Task<ApiResponse<object>> CreateFeedbackAuthorised([FromBody] CreateFeedbackDTO req, CancellationToken
        ct)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<object>.Fail(401, "User should be authorised to leave feedback");
        await _feedbackService.SaveAuthorisedFeedback(req, userId!, ct);
        return ApiResponse<object>.Success(200, null);
    }

    [HttpPost("visitor")]
    public async Task<ApiResponse<object>> CreateFeedbackVisitor([FromBody] CreateFeedbackDTO req, [FromQuery] string
        secretCode, CancellationToken ct)
    {
        await _feedbackService.SaveVisitorFeedback(req, secretCode, ct);
        return ApiResponse<object>.Success(200, null); 
    }
}