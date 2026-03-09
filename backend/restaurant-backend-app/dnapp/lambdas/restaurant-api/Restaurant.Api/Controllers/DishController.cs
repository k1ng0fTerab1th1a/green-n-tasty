using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Mappers;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("dishes")]
public class DishController(IDishService _dishService) : ControllerBase
{
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularDishes()
    {
        var dishesEntities = await _dishService.GetPopularDishesAsync();

        var mappedDishes = dishesEntities.Select(dish => dish.ToShortResponse()).ToList();

        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }
}
