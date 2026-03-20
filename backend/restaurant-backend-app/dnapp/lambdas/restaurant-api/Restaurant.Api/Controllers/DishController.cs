using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Mappers;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("dishes")]
public class DishController(IDishService _dishService) : ControllerBase
{
    [HttpGet("popular")]
    [ProducesResponseType(typeof(ApiResponse<List<DishShortResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularDishes()
    {
        var dishesEntities = await _dishService.GetPopularDishesAsync();

        var mappedDishes = dishesEntities.Select(dish => dish.ToShortResponse()).ToList();

        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDishById(string id, CancellationToken ct)
    {
        var dish = await _dishService.GetDishByIdAsync(id, ct);
        if (dish == null)
            return ApiResponse<Dish>.Fail(404, $"Dish with the id of {id} was not found");

        return ApiResponse<Dish>.Success(200, dish);
    }
}
