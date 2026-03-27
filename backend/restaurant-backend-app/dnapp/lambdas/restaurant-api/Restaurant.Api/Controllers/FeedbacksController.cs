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
        
    }

    /*
    [HttpGet("visitor")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVisitorReservationInfo(
        [FromQuery] string reservationId,
        [FromQuery] string secretCode,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(reservationId) || string.IsNullOrEmpty(secretCode))
            return ApiResponse<object>.Fail(400, "ReservationId and secret code are required");

        var reservation = await _reservationService.GetByIdAsync(reservationId, ct);
        if (reservation == null)
            return ApiResponse<object>.Fail(404, "Reservation not found");

        if (reservation.SecretCode != secretCode)
            return ApiResponse<object>.Fail(401, "Invalid secret code");

        if (reservation.Status != ReservationStatus.InProgress
            && reservation.Status != ReservationStatus.Finished)
            return ApiResponse<object>.Fail(400, "Reservation is not available for feedback");

        var waiter = await _userService.GetByIdAsync(reservation.WaiterId, ct);

        return Ok(new VisitorReservationInfoDTO
        {
            ReservationId = reservationId,
            WaiterName = reservation.WaiterName,
            WaiterImageUrl = waiter?.AvatarUrl,
            ServiceRating = waiter?.AverageServiceRating
        });
    }
    */
}