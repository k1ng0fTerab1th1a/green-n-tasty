using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Api.Extensions;
using Restaurant.Api.Mappers;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("dishes")]
public class DishController(IDishService _dishService, S3FileService _s3FileService) : ControllerBase
{
    [HttpGet("popular")]
    [ProducesResponseType(typeof(ApiResponse<List<DishShortResponse>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<DishShortResponse>>> GetPopularDishes(CancellationToken ct)
    {
        var result = await _dishService.GetPopularDishesAsync(ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<DishShortResponse>>();

        var mappedDishes = result.Value.Select(dish => dish.ToShortResponse()).ToList();

        return ApiResponse<List<DishShortResponse>>.Success(StatusCodes.Status200OK, mappedDishes);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Dish>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<Dish>> GetDishById(string id, CancellationToken ct)
    {
        var result = await _dishService.GetDishByIdAsync(id, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<Dish>();

        return ApiResponse<Dish>.Success(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet("menu")]
    [ProducesResponseType(typeof(ApiResponse<List<DishBriefDTO>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<DishBriefDTO>>> GetMenuDishes(CancellationToken ct, [FromQuery] string? type = null, string sort = 
        "price,asc")
    {
        var result = await _dishService.GetMenuBriefDishesAsync(type, sort, ct);
        if (result.IsFailed)
            return result.Errors[0].ToApiResponse<List<DishBriefDTO>>();

        return ApiResponse<List<DishBriefDTO>>.Success(StatusCodes.Status200OK, result.Value.ToList());
    }

    [HttpGet("menu-file")]
    public async Task<IActionResult> GetMenuFile(CancellationToken ct)
    {
        var stream = await _s3FileService.GetFileStreamAsync("uploads/menu/menu.pdf", ct);
        return File(stream, "application/pdf", enableRangeProcessing:false);
    }

    [HttpGet("menu-url")]
    public async Task<ApiResponse<string>> GetMenuUrl()
    {
        return ApiResponse<string>.Success(200, "https://run20-tm2-frontend-bucket.s3.eu-west-2.amazonaws" +
            ".com/uploads/menu/menu.pdf");
    }
}
