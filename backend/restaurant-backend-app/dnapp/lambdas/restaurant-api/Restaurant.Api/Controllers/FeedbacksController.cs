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
    private readonly Core.Services.QRCoder _qrCoder;

    public FeedbacksController(IReservationService reservationService, IFeedbackService feedbackService)
    {
        _reservationService = reservationService;
        _feedbackService = feedbackService;
        _qrCoder = new Core.Services.QRCoder();
    }
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDTO req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        bool isAnonymous = string.IsNullOrEmpty(userId);

        if (isAnonymous)
        {
            // will be a method for anonymous
            return ApiResponse<object>.Fail(500, "Not yet implemented");
        }
        else
        {
            await _feedbackService.SaveAuthorisedFeedback(req, userId!, ct);
            return ApiResponse<object>.Success(200, null);
        }
    }

    [HttpGet("reservations/{id}/qr")]
    public async Task<IActionResult> GetQrCode(string id)
    {
        var waiterId = User.GetUserId();
        if (waiterId == null)
            return ApiResponse<object>.Fail(401, "You have to be logged in to access this reservation");
        var reservation = await _reservationService.GetByIdAsync(id, waiterId, true);
        
        var url = $"https://yourapp.com/feedback?reservationId={id}&secretCode={reservation.Value.SecretCode!}";
        var pngBytes = _qrCoder.GenerateQrCode(url);

        return File(pngBytes, "image/png");
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