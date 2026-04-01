using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Infrastructure;

namespace Restaurant.Api.Controllers;

[ApiController]
public sealed class DevSeedController : ControllerBase
{
    private readonly IDynamoDBContext _db;
    private readonly IDishService _dishService;

    public DevSeedController(IDynamoDBContext db, IDishService dishService)
    {
        _db = db;
        _dishService = dishService;
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
            CustomerName = req.CustomerName,
            VisitorName = req.VisitorName,
            WaiterId = req.WaiterId,
            WaiterName = req.WaiterName ?? string.Empty,
            LocationId = req.LocationId,
            LocationAddress = req.LocationAddress ?? string.Empty,
            TableNumber = req.TableNumber,
            TableKey = $"{req.LocationId}#{req.TableNumber}",
            StartDateTime = startDto.ToUniversalTime().ToString("O"),
            EndDateTime = endDto.ToUniversalTime().ToString("O"),
            ActualStartTime = req.ActualStartTime,
            ActualEndTime = req.ActualEndTime,
            GuestsCount = req.GuestsCount,
            Status = req.Status ?? ReservationStatus.Reserved,
            SecretCode = req.SecretCode,
            IsCreatedByWaiter = req.IsCreatedByWaiter,
            CreatedAt = nowIso,
            UpdatedAt = nowIso
        };

        await _db.SaveAsync(item, ct);

        return ApiResponse<Reservation>.Success(StatusCodes.Status201Created, item);
    }

    [HttpPost("seed/orders")]
    public async Task<IActionResult> SeedOrder([FromBody] DevSeedOrderRequest req, CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        if (string.IsNullOrWhiteSpace(req.ReservationId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "ReservationId is required.");
        if (string.IsNullOrWhiteSpace(req.WaiterId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "WaiterId is required.");
        if (string.IsNullOrWhiteSpace(req.LocationId))
            return ApiResponse<object>.Fail(StatusCodes.Status400BadRequest, "LocationId is required.");

        var nowIso = DateTimeOffset.UtcNow.ToString("O");

        var item = new Order
        {
            Id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString("N") : req.Id,
            ReservationId = req.ReservationId,
            LocationId = req.LocationId,
            LocationAddress = req.LocationAddress ?? string.Empty,
            WaiterId = req.WaiterId,
            WaiterName = req.WaiterName ?? string.Empty,
            CustomerId = req.CustomerId,
            CustomerName = req.CustomerName,
            VisitorName = req.VisitorName,
            TableNumber = req.TableNumber,
            GuestsCount = req.GuestsCount,
            Status = req.Status ?? OrderStatus.Completed,
            Dishes = req.Dishes ?? [],
            TotalAmount = req.Dishes?.Sum(d => d.PriceAtOrder * d.Quantity) ?? 0,
            CreatedAt = nowIso,
            CompletedAt = nowIso,
        };

        await _db.SaveAsync(item, ct);

        return ApiResponse<Order>.Success(StatusCodes.Status201Created, item);
    }

    [HttpPost("seed/dishes/search-index/rebuild")]
    public async Task<IActionResult> RebuildDishSearchIndex(CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var result = await _dishService.RebuildSearchIndexAsync(ct);
        if (result.IsFailed)
            return ApiResponse<object>.Fail(StatusCodes.Status500InternalServerError, "Failed to rebuild dish search index.");

        return ApiResponse<object>.Success(StatusCodes.Status200OK, new
        {
            indexed = result.Value
        });
    }

    [HttpPost("seed/dishes/{dishId}/search-index")]
    public async Task<IActionResult> UpsertDishSearchIndex([FromRoute] string dishId, CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var result = await _dishService.UpsertDishSearchIndexAsync(dishId, ct);
        if (result.IsFailed)
            return ApiResponse<object>.Fail(StatusCodes.Status404NotFound, "Dish not found.");

        return ApiResponse<object>.Success(StatusCodes.Status200OK, new { dishId });
    }

    [HttpDelete("seed/dishes/{dishId}/search-index")]
    public async Task<IActionResult> RemoveDishSearchIndex([FromRoute] string dishId, CancellationToken ct)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var result = await _dishService.RemoveDishSearchIndexAsync(dishId, ct);
        if (result.IsFailed)
            return ApiResponse<object>.Fail(StatusCodes.Status500InternalServerError, "Failed to remove dish from search index.");

        return ApiResponse<object>.Success(StatusCodes.Status200OK, new { dishId });
    }

    [HttpPost("seed/waiters-list/backfill-from-schedule")]
    public async Task<IActionResult> BackfillWaitersListFromSchedule([FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var schedules = await _db.ScanAsync<WaiterSchedule>(Array.Empty<ScanCondition>()).GetRemainingAsync(ct);
        var users = await _db.ScanAsync<User>(Array.Empty<ScanCondition>()).GetRemainingAsync(ct);
        var waiterListEntries = await _db.ScanAsync<WaiterListEntry>(Array.Empty<ScanCondition>()).GetRemainingAsync(ct);

        var usersById = users
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .ToDictionary(x => x.UserId, x => x);

        var existingWaiterListByEmail = waiterListEntries
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .ToDictionary(x => x.Email.Trim().ToLowerInvariant(), x => x);

        var normalizedCandidates = new Dictionary<string, (string Email, string LocationId, string WaiterId)>();

        var skippedMissingWaiterId = 0;
        var skippedBadTableKey = 0;
        var skippedUserNotFound = 0;
        var skippedUserEmailMissing = 0;
        var skippedUserNotWaiter = 0;
        var skippedMultipleLocationsForSameEmail = 0;

        foreach (var schedule in schedules)
        {
            if (string.IsNullOrWhiteSpace(schedule.WaiterId))
            {
                skippedMissingWaiterId++;
                continue;
            }

            var locationId = ExtractLocationId(schedule.TableKey);
            if (string.IsNullOrWhiteSpace(locationId))
            {
                skippedBadTableKey++;
                continue;
            }

            if (!usersById.TryGetValue(schedule.WaiterId, out var user))
            {
                skippedUserNotFound++;
                continue;
            }

            if (!IsWaiterCandidate(user))
            {
                skippedUserNotWaiter++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                skippedUserEmailMissing++;
                continue;
            }

            var normalizedEmail = user.Email.Trim().ToLowerInvariant();

            if (normalizedCandidates.TryGetValue(normalizedEmail, out var existing))
            {
                if (!string.Equals(existing.LocationId, locationId, StringComparison.Ordinal))
                {
                    skippedMultipleLocationsForSameEmail++;
                    normalizedCandidates.Remove(normalizedEmail);
                }

                continue;
            }

            normalizedCandidates[normalizedEmail] = (user.Email.Trim(), locationId, schedule.WaiterId);
        }

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        var preview = new List<object>();

        foreach (var item in normalizedCandidates.Values.OrderBy(x => x.Email))
        {
            if (existingWaiterListByEmail.TryGetValue(item.Email.Trim().ToLowerInvariant(), out var existingEntry))
            {
                if (string.Equals(existingEntry.LocationId?.Trim(), item.LocationId, StringComparison.Ordinal))
                {
                    unchanged++;
                    continue;
                }

                preview.Add(new
                {
                    action = "update",
                    email = item.Email,
                    oldLocationId = existingEntry.LocationId,
                    newLocationId = item.LocationId,
                    waiterId = item.WaiterId
                });

                if (!dryRun)
                {
                    existingEntry.LocationId = item.LocationId;
                    await _db.SaveAsync(existingEntry, ct);
                    updated++;
                }

                continue;
            }

            preview.Add(new
            {
                action = "create",
                email = item.Email,
                locationId = item.LocationId,
                waiterId = item.WaiterId
            });

            if (!dryRun)
            {
                await _db.SaveAsync(new WaiterListEntry
                {
                    Email = item.Email,
                    LocationId = item.LocationId
                }, ct);

                created++;
            }
        }

        return ApiResponse<object>.Success(StatusCodes.Status200OK, new
        {
            dryRun,
            totalSchedulesScanned = schedules.Count,
            totalUsersScanned = users.Count,
            totalWaiterListEntriesScanned = waiterListEntries.Count,
            candidateEmails = normalizedCandidates.Count,
            created,
            updated,
            unchanged,
            skippedMissingWaiterId,
            skippedBadTableKey,
            skippedUserNotFound,
            skippedUserEmailMissing,
            skippedUserNotWaiter,
            skippedMultipleLocationsForSameEmail,
            preview = preview.Take(50).ToList()
        });
    }

    [HttpPost("seed/users/waiters-location/backfill")]
    public async Task<IActionResult> BackfillWaiterLocations([FromQuery] bool dryRun = true, CancellationToken ct = default)
    {
        if (!IsAllowed())
            return ApiResponse<object>.Fail(StatusCodes.Status403Forbidden, "Seed endpoint is disabled.");

        var waiterEntries = await _db.ScanAsync<WaiterListEntry>(Array.Empty<ScanCondition>()).GetRemainingAsync(ct);
        var users = await _db.ScanAsync<User>(Array.Empty<ScanCondition>()).GetRemainingAsync(ct);

        var waiterLocationByEmail = waiterEntries
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email.Trim().ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(x => x.LocationId?.Trim())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            );

        var waiterUsers = users
            .Where(IsWaiterUser)
            .ToList();

        var skippedAlreadyHasLocation = 0;
        var skippedNoEmail = 0;
        var skippedNoWaiterListMatch = 0;
        var skippedWaiterListLocationMissing = 0;
        var matchedUsers = 0;
        var updatedUsers = 0;

        var preview = new List<object>();

        foreach (var user in waiterUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.LocationId))
            {
                skippedAlreadyHasLocation++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                skippedNoEmail++;
                continue;
            }

            var normalizedEmail = user.Email.Trim().ToLowerInvariant();

            if (!waiterLocationByEmail.TryGetValue(normalizedEmail, out var locationId))
            {
                skippedNoWaiterListMatch++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(locationId))
            {
                skippedWaiterListLocationMissing++;
                continue;
            }

            matchedUsers++;

            preview.Add(new
            {
                userId = user.UserId,
                email = user.Email,
                locationId
            });

            if (dryRun)
                continue;

            user.LocationId = locationId;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");

            await _db.SaveAsync(user, ct);
            updatedUsers++;
        }

        return ApiResponse<object>.Success(StatusCodes.Status200OK, new
        {
            dryRun,
            totalUsersScanned = users.Count,
            waiterUsersFound = waiterUsers.Count,
            waiterListEntriesScanned = waiterEntries.Count,
            matchedUsers,
            updatedUsers,
            skippedAlreadyHasLocation,
            skippedNoEmail,
            skippedNoWaiterListMatch,
            skippedWaiterListLocationMissing,
            preview = preview.Take(50).ToList()
        });
    }

    private static bool IsWaiterUser(User user)
    {
        return string.Equals(user.Role, "WAITER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.WaiterFlag, "1", StringComparison.Ordinal);
    }

    private static string? ExtractLocationId(string? tableKey)
    {
        if (string.IsNullOrWhiteSpace(tableKey))
            return null;

        var index = tableKey.IndexOf('#');
        if (index <= 0)
            return null;

        var locationId = tableKey[..index].Trim();
        return string.IsNullOrWhiteSpace(locationId) ? null : locationId;
    }

    private static bool IsWaiterCandidate(User user)
    {
        return string.Equals(user.Role, "WAITER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.WaiterFlag, "1", StringComparison.Ordinal);
    }
}

public sealed class DevSeedReservationRequest
{
    public string? Id { get; set; }

    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? VisitorName { get; set; }
    public string? SecretCode { get; set; }
    public bool IsCreatedByWaiter { get; set; }
    public string WaiterId { get; set; } = null!;
    public string? WaiterName { get; set; }
    public string LocationId { get; set; } = null!;
    public string? LocationAddress { get; set; }
    public int TableNumber { get; set; }

    public string StartDateTime { get; set; } = null!;
    public string EndDateTime { get; set; } = null!;
    public string? ActualStartTime { get; set; }
    public string? ActualEndTime { get; set; }

    public int GuestsCount { get; set; }

    public ReservationStatus? Status { get; set; }
}

public sealed class DevSeedOrderRequest
{
    public string? Id { get; set; }
    public string ReservationId { get; set; } = null!;
    public string LocationId { get; set; } = null!;
    public string? LocationAddress { get; set; }
    public string WaiterId { get; set; } = null!;
    public string? WaiterName { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? VisitorName { get; set; }
    public int TableNumber { get; set; }
    public int GuestsCount { get; set; }
    public OrderStatus? Status { get; set; }
    public List<OrderDishSnapshot>? Dishes { get; set; }
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