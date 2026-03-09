using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
namespace Restaurant.Api.Controllers;

[ApiController]
[Route("locations")]
public sealed class LocationsController(IFeedbackService _feedbackService, ILocationService _locationService, IDishService _dishService) : ControllerBase
{
    [HttpGet("{id}/feedbacks")]
    public async Task<IActionResult> GetFeedbacksByLocationId(string id, string type, [FromQuery] List<string> sort, int size = 20, string? pageToken = null)
    {
        if (sort.Count == 0)
        {
            sort.Add("date,asc");
        }

        var feedbackResponse = await _feedbackService.GetFeedbacksForLocation(id, size, type, sort, pageToken);
        return ApiResponse<FeedbackPaginatedDto>.Success(StatusCodes.Status200OK, feedbackResponse);
    }


    [HttpGet]
    public async Task<IActionResult> GetLocations(CancellationToken cancellationToken)
    {
        var locations = await _locationService.GetLocationsAsync(cancellationToken);
        return ApiResponse<LocationResponse[]>.Success(StatusCodes.Status200OK, locations.Select(l => l.ToResponse()).ToArray());
    }

    [HttpGet("{id}/speciality-dishes")]
    public async Task<IActionResult> GetSpecialityDishes([FromRoute] string id, CancellationToken cancellationToken)
    {
        var dishesEntities = await _dishService.GetSpecialityDishesByLocationIdAsync(id, cancellationToken);

        var mappedDishes = dishesEntities.Select(dish => dish.ToShortResponse()).ToList();

        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }

    [HttpGet("select-options")]
    public async Task<IActionResult> GetLocationOptions(CancellationToken cancellationToken)
    {
        var options = await _locationService.GetLocationOptionsAsync(cancellationToken);

        var response = options.Select(o => new LocationBrief(o.Id, o.Address)).ToArray();

        return ApiResponse<LocationBrief[]>.Success(StatusCodes.Status200OK, response);
    }
}
