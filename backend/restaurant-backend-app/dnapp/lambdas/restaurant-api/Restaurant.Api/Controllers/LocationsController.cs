using Microsoft.AspNetCore.Mvc;
using Restaurant.Core.Interfaces;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("locations")]
public class LocationsController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpGet("{id}/feedbacks")]
    public async Task<ActionResult> GetFeedbacksByLocationId(string id, string type, [FromQuery] List<string> sort, int page = 0, int size = 20, string? pageToken = null )
    {
        if (sort.Count == 0)
        {
            sort.Add("date,asc");
        }

        var feedbackResponse = await feedbackService.GetFeedbacksForLocation(id, size, type, sort, pageToken);

        feedbackResponse.Number = page;

        return Ok(feedbackResponse);
    }
}