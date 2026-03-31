using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.IntegrationTests.Infrastructure;

[Collection("DynamoDb collection")]
public sealed class FeedbackRepositoryIntegrationTests
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;
    private readonly FeedbackRepository _repo;

    public FeedbackRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _client  = fixture.Client;
        _repo    = new FeedbackRepository(_context, _client);
    }


    [Fact]
    public async Task SaveBatchAsync_ShouldPersistAllFeedbacks_AndSetLocationIdAndType()
    {
        var locationId    = $"loc-{Guid.NewGuid():N}";
        var reservationId = Guid.NewGuid().ToString("N");

        var feedbacks = new List<Feedback>
        {
            new()
            {
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 5,
                Comment       = "Excellent waiter",
                UserId        = "user-1",
                UserName      = "Ana K.",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = locationId,
                Type          = "waiter"
            },
            new()
            {
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 4,
                Comment       = "Good food",
                UserId        = "user-1",
                UserName      = "Ana K.",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = locationId,
                Type          = "kitchen"
            }
        };

        await _repo.SaveBatchAsync(feedbacks);

        var waiter  = await _context.LoadAsync<Feedback>(feedbacks[0].Id);
        var kitchen = await _context.LoadAsync<Feedback>(feedbacks[1].Id);

        waiter.Should().NotBeNull();
        waiter!.Rate.Should().Be(5);
        waiter.LocationIdAndType.Should().Be($"{locationId}#waiter");

        kitchen.Should().NotBeNull();
        kitchen!.Rate.Should().Be(4);
        kitchen.LocationIdAndType.Should().Be($"{locationId}#kitchen");
    }

    [Fact]
    public async Task SaveBatchAsync_ShouldOverwriteLocationIdAndType_EvenIfAlreadySet()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";

        var feedback = new Feedback
        {
            Id                = Guid.NewGuid().ToString("N"),
            ReservationId     = Guid.NewGuid().ToString("N"),
            Rate              = 3,
            Comment           = "Okay",
            UserId            = "user-2",
            UserName          = "Giorgi B.",
            UserAvatarUrl     = string.Empty,
            Date              = DateTimeOffset.UtcNow.ToString("o"),
            LocationId        = locationId,
            Type              = "waiter",
            LocationIdAndType = "stale-value-should-be-overwritten"
        };

        await _repo.SaveBatchAsync(new[] { feedback });

        var loaded = await _context.LoadAsync<Feedback>(feedback.Id);
        loaded!.LocationIdAndType.Should().Be($"{locationId}#waiter");
    }

    [Fact]
    public async Task GetSecretCodeByReservationIdAsync_WhenSecretCodeExists_ShouldReturnIt()
    {
        var reservationId = Guid.NewGuid().ToString("N");

        var reservation = new Reservation
        {
            Id           = reservationId,
            CustomerId   = null,
            CustomerName = null,
            WaiterId     = $"waiter-{Guid.NewGuid():N}",
            WaiterName   = "Giorgi B.",
            LocationId   = $"loc-{Guid.NewGuid():N}",
            TableNumber  = 7,
            TableKey     = $"loc-secret#7",
            StartDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            EndDateTime   = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            GuestsCount   = 2,
            Status        = ReservationStatus.InProgress,
            SecretCode    = "ALPHA-7X",
            CreatedAt     = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt     = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);

        var code = await _repo.GetSecretCodeByReservationIdAsync(reservationId, CancellationToken.None);

        code.Should().Be("ALPHA-7X");
    }

    [Fact]
    public async Task GetSecretCodeByReservationIdAsync_WhenSecretCodeIsAbsent_ShouldReturnNull()
    {
        var reservationId = Guid.NewGuid().ToString("N");

        var reservation = new Reservation
        {
            Id            = reservationId,
            CustomerId    = null,
            CustomerName  = null,
            WaiterId      = $"waiter-{Guid.NewGuid():N}",
            WaiterName    = "Giorgi B.",
            LocationId    = $"loc-{Guid.NewGuid():N}",
            TableNumber   = 3,
            TableKey      = $"loc-nosecret#3",
            StartDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            EndDateTime   = DateTimeOffset.UtcNow.AddHours(3).ToString("O"),
            GuestsCount   = 2,
            Status        = ReservationStatus.Reserved,
            SecretCode    = null,   // explicitly no secret
            CreatedAt     = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt     = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);

        var code = await _repo.GetSecretCodeByReservationIdAsync(reservationId, CancellationToken.None);

        code.Should().BeNull();
    }

    [Fact]
    public async Task GetSecretCodeByReservationIdAsync_WhenReservationDoesNotExist_ShouldReturnNull()
    {
        var nonExistentId = Guid.NewGuid().ToString("N");

        var code = await _repo.GetSecretCodeByReservationIdAsync(nonExistentId, CancellationToken.None);

        code.Should().BeNull();
    }



    [Fact]
    public async Task GetByLocationAsync_ShouldReturnOnlyFeedbacksMatchingLocationAndType()
    {
        var locationId      = $"loc-{Guid.NewGuid():N}";
        var otherLocationId = $"loc-{Guid.NewGuid():N}";
        var reservationId   = Guid.NewGuid().ToString("N");

        var feedbacks = new List<Feedback>
        {
            new()
            {
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 5,
                Comment       = "Matching waiter feedback",
                UserId        = "user-6",
                UserName      = "Test",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = locationId,
                Type          = "waiter"
            },
            new()
            {
                // same location, wrong type — must not appear
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 4,
                Comment       = "Kitchen feedback, should be excluded",
                UserId        = "user-6",
                UserName      = "Test",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = locationId,
                Type          = "kitchen"
            },
            new()
            {
                // different location — must not appear
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = Guid.NewGuid().ToString("N"),
                Rate          = 3,
                Comment       = "Different location feedback",
                UserId        = "user-7",
                UserName      = "Other",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = otherLocationId,
                Type          = "waiter"
            }
        };

        await _repo.SaveBatchAsync(feedbacks);

        var result = await _repo.GetByLocationAsync(locationId, size: 10, type: "waiter");

        result.Feedbacks.Should().NotBeEmpty();
        result.Feedbacks.Should().OnlyContain(f => f.LocationId == locationId && f.Type == "waiter");
    }

    [Fact]
    public async Task GetByLocationAsync_WhenNoFeedbacksExist_ShouldReturnEmptyList()
    {
        var locationId = $"loc-{Guid.NewGuid():N}"; // never saved to

        var result = await _repo.GetByLocationAsync(locationId, size: 10, type: "waiter");

        result.Feedbacks.Should().BeEmpty();
        result.NextPageToken.Should().BeNull();
    }

    [Fact]
    public async Task GetByLocationAsync_ShouldRespectSizeLimit_AndReturnNextPageToken()
    {
        var locationId    = $"loc-{Guid.NewGuid():N}";
        var reservationId = Guid.NewGuid().ToString("N");

        var feedbacks = Enumerable.Range(1, 5).Select(i => new Feedback
        {
            Id            = Guid.NewGuid().ToString("N"),
            ReservationId = reservationId,
            Rate          = i,
            Comment       = $"Feedback {i}",
            UserId        = $"user-page-{i}",
            UserName      = $"User {i}",
            UserAvatarUrl = string.Empty,
            Date          = DateTimeOffset.UtcNow.AddMinutes(i).ToString("o"),
            LocationId    = locationId,
            Type          = "waiter"
        }).ToList();

        await _repo.SaveBatchAsync(feedbacks);

        var firstPage = await _repo.GetByLocationAsync(locationId, size: 3, type: "waiter");

        firstPage.Feedbacks.Should().HaveCount(3);
        firstPage.NextPageToken.Should().NotBeNullOrEmpty();

        var secondPage = await _repo.GetByLocationAsync(
            locationId, size: 3, type: "waiter", pageToken: firstPage.NextPageToken);

        secondPage.Feedbacks.Should().HaveCountGreaterThan(0);
        secondPage.Feedbacks.Should().NotIntersectWith(
            firstPage.Feedbacks,
            because: "pages should not overlap");
    }

    [Fact]
    public async Task GetByLocationAsync_WhenSortByRateDesc_ShouldUseRateIndex()
    {
        var locationId    = $"loc-{Guid.NewGuid():N}";
        var reservationId = Guid.NewGuid().ToString("N");

        var feedbacks = new List<Feedback>
        {
            new()
            {
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 2,
                Comment       = "Low rating",
                UserId        = "user-sort-1",
                UserName      = "User A",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.ToString("o"),
                LocationId    = locationId,
                Type          = "kitchen"
            },
            new()
            {
                Id            = Guid.NewGuid().ToString("N"),
                ReservationId = reservationId,
                Rate          = 5,
                Comment       = "High rating",
                UserId        = "user-sort-2",
                UserName      = "User B",
                UserAvatarUrl = string.Empty,
                Date          = DateTimeOffset.UtcNow.AddMinutes(1).ToString("o"),
                LocationId    = locationId,
                Type          = "kitchen"
            }
        };

        await _repo.SaveBatchAsync(feedbacks);

        // desc means ScanIndexForward = false → highest rate first
        var result = await _repo.GetByLocationAsync(
            locationId, size: 10, type: "kitchen", sort: new List<string> { "rate,desc" });

        result.Feedbacks.Should().NotBeEmpty();
        result.Feedbacks[0].Rate.Should().BeGreaterThanOrEqualTo(result.Feedbacks[^1].Rate);
    }
}