using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("dishes")]
public class DishController(IDishService _dishService) : ControllerBase
{
    [HttpGet("popular")]
    [ProducesResponseType(typeof(ApiResponse<List<DishShortResponse>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<DishShortResponse>>> GetPopularDishes()
    {
        var result = await _dishService.GetPopularDishesAsync();
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<DishShortResponse>>();

        var mappedDishes = result.Value.Select(dish => dish.ToShortResponse()).ToList();

        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }
}
