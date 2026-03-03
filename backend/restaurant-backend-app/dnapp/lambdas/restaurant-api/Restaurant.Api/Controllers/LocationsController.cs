using Microsoft.AspNetCore.Mvc;
using Restaurant.Core.Interfaces;
using Restaurant.Infrastructure.Utils;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("locations")]
public class LocationsController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpGet("{id}/feedbacks")]
    public async Task<ActionResult> GetFeedbacksByLocationId(string id, string type, [FromQuery] List<string> sort, int size = 20, string? pageToken = null )
    {
        if (sort.Count == 0)
        {
            sort.Add("date,asc");
        }

        var feedbackResponse = await feedbackService.GetFeedbacksForLocation(id, size, type, sort, pageToken);
        return Ok(feedbackResponse);
    }
}