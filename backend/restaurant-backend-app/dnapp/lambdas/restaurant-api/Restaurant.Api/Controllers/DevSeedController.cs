using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Models;
using Restaurant.Core.Models;

namespace Restaurant.Api.Controllers;

[ApiController]
public sealed class DevSeedController : ControllerBase
{
    private readonly IDynamoDBContext _db;

    public DevSeedController(IDynamoDBContext db)
    {
        _db = db;
    }

    private bool IsAllowed()
    {
        var expected = Environment.GetEnvironmentVariable("SEED_TOKEN");
        if (string.IsNullOrWhiteSpace(expected))
            return false;

        if (!Request.Headers.TryGetValue("X-Seed-Token", out var provided))
            return false;

        return string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }

    [HttpPost("seed/locations")]
    public async Task<IActionResult> SeedLocation([FromBody] CreateLocationRequest req, CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var item = new Location
        {
            Id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString("N") : req.Id,
            EntityType = "LOCATION", 
            Address = req.Address,
            Description = req.Description,
            TotalCapacity = req.TotalCapacity,
            AverageOccupancy = req.AverageOccupancy,
            ImageUrl = req.ImageUrl,
            Rating = req.Rating
        };

        await _db.SaveAsync(item, ct);

        return ApiResponse<Location>.Success(201, item);
    }
}

public sealed class CreateLocationRequest
{
    public string? Id { get; set; }
    public string Address { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int TotalCapacity { get; set; }
    public double AverageOccupancy { get; set; }
    public string ImageUrl { get; set; } = null!;
    public double Rating { get; set; }
}