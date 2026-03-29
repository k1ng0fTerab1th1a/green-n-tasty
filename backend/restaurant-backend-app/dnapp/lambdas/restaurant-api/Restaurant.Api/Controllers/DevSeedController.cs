using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
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
            Address = req.Address,
            Description = req.Description,
            TotalCapacity = req.TotalCapacity,
            AverageOccupancy = req.AverageOccupancy,
            ImageUrl = req.ImageUrl,
            TotalRating = req.Rating
        };

        await _db.SaveAsync(item, ct);

        return ApiResponse<Location>.Success(201, item);
    }

    [HttpPost("seed/reservations")]
    public async Task<IActionResult> SeedReservation([FromBody] DevSeedReservationRequest req, CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        if (string.IsNullOrWhiteSpace(req.CustomerId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "CustomerId is required.");
        if (string.IsNullOrWhiteSpace(req.WaiterId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "WaiterId is required.");
        if (string.IsNullOrWhiteSpace(req.LocationId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "LocationId is required.");
        if (req.TableNumber <= 0)
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "TableNumber must be > 0.");
        if (req.GuestsCount <= 0)
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "GuestsCount must be > 0.");
        if (string.IsNullOrWhiteSpace(req.StartDateTime) || string.IsNullOrWhiteSpace(req.EndDateTime))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "StartDateTime and EndDateTime are required.");

        if (!DateTimeOffset.TryParse(req.StartDateTime, out var startDto) ||
            !DateTimeOffset.TryParse(req.EndDateTime, out var endDto))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "StartDateTime/EndDateTime must be valid ISO-8601.");
        if (endDto <= startDto)
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "EndDateTime must be after StartDateTime.");

        var nowIso = DateTimeOffset.UtcNow.ToString("O");

        var item = new Reservation
        {
            Id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString("N") : req.Id,
            CustomerId = req.CustomerId,
            WaiterId = req.WaiterId,
            LocationId = req.LocationId,
            TableNumber = req.TableNumber,
            TableKey = $"{req.LocationId}#{req.TableNumber}",
            StartDateTime = startDto.ToUniversalTime().ToString("O"),
            EndDateTime = endDto.ToUniversalTime().ToString("O"),
            GuestsCount = req.GuestsCount,
            Status = req.Status ?? ReservationStatus.Reserved,
            CreatedAt = nowIso,
            UpdatedAt = nowIso
        };

        await _db.SaveAsync(item, ct);

        return ApiResponse<Reservation>.Success(StatusCodes.Status201Created, item);
    }
}

public sealed class DevSeedReservationRequest
{
    public string? Id { get; set; }

    public string CustomerId { get; set; } = null!;
    public string WaiterId { get; set; } = null!;
    public string LocationId { get; set; } = null!;
    public int TableNumber { get; set; }

    public string StartDateTime { get; set; } = null!;
    public string EndDateTime { get; set; } = null!;

    public int GuestsCount { get; set; }

    public ReservationStatus? Status { get; set; }
}

public sealed class CreateLocationRequest
{
    public string? Id { get; set; }
    public string Address { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int TotalCapacity { get; set; }
    public double AverageOccupancy { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int Rating { get; set; }
}