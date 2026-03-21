using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("dishes")]
public class DishController(IDishService _dishService, S3FileService _s3FileService) : ControllerBase
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

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenuDishes(CancellationToken ct, [FromQuery] string? type = null, string sort = 
        "price,asc")
    {
        var dishes = await _dishService.GetMenuBriefDishesAsync(type, sort, ct);
        return ApiResponse<List<DishBriefDTO>>.Success(200, dishes.ToList());
    }

    [HttpGet("menu-file")]
    public async Task<IActionResult> GetMenuFile(CancellationToken ct)
    {
        var stream = await _s3FileService.GetFileStreamAsync("restaurant-menu/menu.pdf", ct);
        return File(stream, "application/pdf");
    }
}
