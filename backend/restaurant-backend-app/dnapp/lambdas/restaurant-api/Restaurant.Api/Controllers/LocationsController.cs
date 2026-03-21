using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("locations")]
public sealed class LocationsController(IFeedbackService _feedbackService, ILocationService _locationService, IDishService _dishService) : ControllerBase
{
    [HttpGet("{id}/feedbacks")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackPaginatedDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<FeedbackPaginatedDto>> GetFeedbacksByLocationId(string id, string type, [FromQuery] List<string> sort, int size = 20, string? pageToken = null)
    {
        if (sort.Count == 0)
        {
            sort.Add("date,asc");
        }

        var result = await _feedbackService.GetFeedbacksForLocation(id, size, type, sort, pageToken);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<FeedbackPaginatedDto>();

        return ApiResponse<FeedbackPaginatedDto>.Success(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<LocationResponse[]>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<LocationResponse[]>> GetLocations(CancellationToken cancellationToken)
    {
        var result = await _locationService.GetLocationsAsync(cancellationToken);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<LocationResponse[]>();

        return ApiResponse<LocationResponse[]>.Success(StatusCodes.Status200OK, result.Value.Select(l => l.ToResponse()).ToArray());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<LocationResponse>> GetLocationById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _locationService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<LocationResponse>();

        return ApiResponse<LocationResponse>.Success(StatusCodes.Status200OK, result.Value.ToResponse());
    }

    [HttpGet("{id}/speciality-dishes")]
    [ProducesResponseType(typeof(ApiResponse<DishShortResponse>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<DishShortResponse>>> GetSpecialityDishes([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await _dishService.GetSpecialityDishesByLocationIdAsync(id, cancellationToken);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<DishShortResponse>>();

        var mappedDishes = result.Value.Select(dish => dish.ToShortResponse()).ToList();
        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }

    [HttpGet("select-options")]
    [ProducesResponseType(typeof(ApiResponse<LocationBrief[]>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<LocationBrief[]>> GetLocationOptions(CancellationToken cancellationToken)
    {
        var result = await _locationService.GetLocationOptionsAsync(cancellationToken);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<LocationBrief[]>();

        var response = result.Value.Select(o => new LocationBrief(o.Id, o.Address)).ToArray();

        return ApiResponse<LocationBrief[]>.Success(StatusCodes.Status200OK, response);
    }
}
