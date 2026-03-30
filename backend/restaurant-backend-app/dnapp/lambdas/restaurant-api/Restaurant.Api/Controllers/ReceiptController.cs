using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Extensions;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("reservations/{reservationId}/receipt")]
public class ReceiptController(IReceiptService receiptService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = "WAITER")]
    public async Task<IActionResult> GetReceipt([FromRoute] string reservationId, CancellationToken ct)
    {
        var waiterId = User.GetUserId()!;
        var result = await receiptService.GetReceiptAsync(reservationId, waiterId, ct);

        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<object>();

        return File(result.Value, "application/pdf", $"receipt-{reservationId}.pdf");
    }
}
