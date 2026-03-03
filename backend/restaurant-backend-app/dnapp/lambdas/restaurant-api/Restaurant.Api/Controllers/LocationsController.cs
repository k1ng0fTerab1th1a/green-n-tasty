using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.DTOs;
using Restaurant.Api.Models;
using Restaurant.Core.Interfaces;

namespace Restaurant.Api.Controllers
{
    [ApiController]
    [Route("locations")]
    public sealed class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationsController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(CancellationToken cancellationToken)
        {
            var locations = await _locationService.GetLocationsAsync(cancellationToken);

            var response = locations.Select(l => new LocationResponse(
                Id: l.Id,
                Address: l.Address,
                Description: l.Description,
                TotalCapacity: l.TotalCapacity.ToString(CultureInfo.InvariantCulture),
                AverageOccupancy: $"{Math.Round(l.AverageOccupancy * 100, 0).ToString(CultureInfo.InvariantCulture)}%",
                ImageUrl: l.ImageUrl,
                Rating: l.Rating.ToString("0.0", CultureInfo.InvariantCulture)
            )).ToArray();

            return ApiResponse<LocationResponse[]>.Success(StatusCodes.Status200OK, response);
        }

        [HttpGet("{id}/speciality-dishes")]
        public async Task<IActionResult> GetSpecialityDishes([FromRoute] string id, CancellationToken cancellationToken)
        {
            var dishes = await _locationService.GetSpecialityDishesAsync(id, cancellationToken);

            var response = dishes.Select(d => new DishResponse(d.Name, d.Price, d.Weight, d.ImageUrl)).ToArray();

            return ApiResponse<DishResponse[]>.Success(StatusCodes.Status200OK, response);
        }

        [HttpGet("select-options")]
        public async Task<IActionResult> GetLocationOptions(CancellationToken cancellationToken)
        {
            var options = await _locationService.GetLocationOptionsAsync(cancellationToken);

            var response = options.Select(o => new LocationBrief(o.Id, o.Address)).ToArray();

            return ApiResponse<LocationBrief[]>.Success(StatusCodes.Status200OK, response);
        }
    }
}
