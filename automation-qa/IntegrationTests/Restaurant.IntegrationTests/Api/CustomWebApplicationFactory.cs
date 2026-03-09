using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeReservationService ReservationService { get; } = new();
    public FakeLocationService LocationService { get; } = new();
    public FakeFeedbackService FeedbackService { get; } = new();
    public FakeDishService DishService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.PostConfigureAll<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
            });

            services.RemoveAll<IAmazonDynamoDB>();
            services.RemoveAll<IDynamoDBContext>();
            services.RemoveAll<IAmazonCognitoIdentityProvider>();

            services.AddSingleton<IAmazonDynamoDB>(_ => new Mock<IAmazonDynamoDB>().Object);
            services.AddSingleton<IDynamoDBContext>(_ => new Mock<IDynamoDBContext>().Object);
            services.AddSingleton<IAmazonCognitoIdentityProvider>(_ => new Mock<IAmazonCognitoIdentityProvider>().Object);

            services.RemoveAll<IReservationService>();
            services.AddSingleton<IReservationService>(ReservationService);

            services.RemoveAll<ILocationService>();
            services.AddSingleton<ILocationService>(LocationService);

            services.RemoveAll<IFeedbackService>();
            services.AddSingleton<IFeedbackService>(FeedbackService);

            services.RemoveAll<IDishService>();
            services.AddSingleton<IDishService>(DishService);
        });
    }

    public sealed class FakeReservationService : IReservationService
    {
        public List<Reservation> SeedReservations { get; } = new();

        public FakeReservationService()
        {
            Reset();
        }

        public void Reset()
        {
            SeedReservations.Clear();

            SeedReservations.Add(new Reservation
            {
                Id = "r-customer-1",
                CustomerId = "customer-1",
                WaiterId = "waiter-1",
                LocationId = "loc-1",
                TableNumber = 3,
                TableKey = "loc-1#3",
                StartDateTime = "2026-03-05T10:00:00.0000000Z",
                EndDateTime = "2026-03-05T11:30:00.0000000Z",
                GuestsCount = 2,
                Status = ReservationStatus.Reserved,
                CreatedAt = "2026-03-01T00:00:00.0000000Z",
                UpdatedAt = "2026-03-01T00:00:00.0000000Z"
            });

            SeedReservations.Add(new Reservation
            {
                Id = "r-customer-2",
                CustomerId = "customer-2",
                WaiterId = "waiter-1",
                LocationId = "loc-2",
                TableNumber = 7,
                TableKey = "loc-2#7",
                StartDateTime = "2026-03-06T12:00:00.0000000Z",
                EndDateTime = "2026-03-06T13:30:00.0000000Z",
                GuestsCount = 4,
                Status = ReservationStatus.Reserved,
                CreatedAt = "2026-03-02T00:00:00.0000000Z",
                UpdatedAt = "2026-03-02T00:00:00.0000000Z"
            });
        }

        public Task<IReadOnlyList<Reservation>> GetMyAsync(string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(actorUserId))
                return Task.FromResult<IReadOnlyList<Reservation>>(Array.Empty<Reservation>());

            var result = actorIsWaiter
                ? SeedReservations.Where(x => x.WaiterId == actorUserId).OrderBy(x => x.Id).ToList()
                : SeedReservations.Where(x => x.CustomerId == actorUserId).OrderBy(x => x.Id).ToList();

            return Task.FromResult<IReadOnlyList<Reservation>>(result);
        }

        public Task<Reservation?> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
        {
            var entity = SeedReservations.SingleOrDefault(x => x.Id == id);
            if (entity is null)
                return Task.FromResult<Reservation?>(null);

            var allowed = entity.CustomerId == actorUserId
                          || (actorIsWaiter && entity.WaiterId == actorUserId);

            if (!allowed)
                throw new UnauthorizedAccessException("Forbidden.");

            return Task.FromResult<Reservation?>(entity);
        }

        public Task<bool> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Reservation?> UpdateReservationAsync(string actorUserId, bool isActorWaiter, UpdateReservationDTO dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class FakeLocationService : ILocationService
    {
        public List<Location> Locations { get; } = new();
        public List<Location> Options { get; } = new();

        public FakeLocationService()
        {
            Reset();
        }

        public void Reset()
        {
            Locations.Clear();
            Options.Clear();

            var item = new Location
            {
                Id = "loc-1",
                EntityType = "LOCATION",
                Address = "Main street 1",
                Description = "Test location",
                TotalCapacity = 120,
                AverageOccupancy = 0.35,
                ImageUrl = "http://img/loc-1",
                Rating = 4.6
            };

            Locations.Add(item);

            Options.Add(new Location
            {
                Id = item.Id,
                EntityType = item.EntityType,
                Address = item.Address,
                Description = item.Description,
                TotalCapacity = item.TotalCapacity,
                AverageOccupancy = item.AverageOccupancy,
                ImageUrl = item.ImageUrl,
                Rating = item.Rating
            });
        }

        public Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Location>>(Locations.ToList());

        public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Location>>(Options.ToList());
    }

    public sealed class FakeFeedbackService : IFeedbackService
    {
        public string? LastLocationId { get; private set; }
        public int LastSize { get; private set; }
        public string? LastType { get; private set; }
        public List<string> LastSort { get; private set; } = new();
        public string? LastPageToken { get; private set; }

        public FeedbackPaginatedDto Response { get; set; } = new();

        public FakeFeedbackService()
        {
            Reset();
        }

        public void Reset()
        {
            LastLocationId = null;
            LastSize = 0;
            LastType = null;
            LastSort = new List<string>();
            LastPageToken = null;

            Response = new FeedbackPaginatedDto
            {
                Size = 20,
                Content = new List<FeedbackDTO>(),
                NextPageToken = null
            };
        }

        public Task<FeedbackPaginatedDto> GetFeedbacksForLocation(
            string locationId,
            int size,
            string type,
            List<string> sort,
            string? pageToken = null)
        {
            LastLocationId = locationId;
            LastSize = size;
            LastType = type;
            LastSort = sort.ToList();
            LastPageToken = pageToken;

            return Task.FromResult(Response);
        }
    }

    public sealed class FakeDishService : IDishService
    {
        public string? LastLocationId { get; private set; }
        public Dictionary<string, IReadOnlyList<Dish>> DishesByLocation { get; } = new();

        public FakeDishService()
        {
            Reset();
        }

        public void Reset()
        {
            LastLocationId = null;
            DishesByLocation.Clear();
            DishesByLocation["loc-1"] = Array.Empty<Dish>();
        }

        public Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(
            string locationId,
            CancellationToken cancellationToken = default)
        {
            LastLocationId = locationId;

            if (DishesByLocation.TryGetValue(locationId, out var dishes))
                return Task.FromResult(dishes);

            return Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());
        }

        public Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());
    }
}
