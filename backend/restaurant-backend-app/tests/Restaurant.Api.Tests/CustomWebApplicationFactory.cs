using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Amazon.CognitoIdentityProvider;
using Moq;
using Restaurant.Api;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Api.Tests
{
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
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
                services.AddSingleton<IReservationService>(new FakeReservationService());

                services.RemoveAll<ILocationService>();
                services.AddSingleton<ILocationService>(new FakeLocationService());

                services.RemoveAll<IFeedbackService>();
                services.AddSingleton<IFeedbackService>(new FakeFeedbackService());

                services.RemoveAll<IDishService>();
                services.AddSingleton<IDishService>(new FakeDishService());
            });
        }

        private sealed class FakeReservationService : IReservationService
        {
            public Task<IReadOnlyList<Reservation>> GetMyAsync(string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
            {
                if (actorUserId == "customer-empty")
                    return Task.FromResult<IReadOnlyList<Reservation>>(Array.Empty<Reservation>());

                var tableNumber = actorIsWaiter ? 99 : 3;

                return Task.FromResult<IReadOnlyList<Reservation>>(new List<Reservation>
                {
                    new()
                    {
                        Id = "r1",
                        CustomerId = actorUserId,
                        WaiterId = "waiter-1",
                        LocationId = "loc-1",
                        TableNumber = tableNumber,
                        TableKey = $"loc-1#{tableNumber}",
                        StartDateTime = "2026-03-05T10:00:00.0000000Z",
                        EndDateTime = "2026-03-05T11:30:00.0000000Z",
                        GuestsCount = 2,
                        Status = ReservationStatus.Reserved,
                        CreatedAt = "2026-03-01T00:00:00.0000000Z",
                        UpdatedAt = "2026-03-01T00:00:00.0000000Z"
                    }
                });
            }

            public Task<Reservation?> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
            {
                if (id == "missing")
                    return Task.FromResult<Reservation?>(null);

                if (id == "forbidden")
                    throw new UnauthorizedAccessException("Forbidden.");

                var tableNumber = actorIsWaiter ? 99 : 3;

                return Task.FromResult<Reservation?>(new Reservation
                {
                    Id = id,
                    CustomerId = actorUserId,
                    WaiterId = "waiter-1",
                    LocationId = "loc-1",
                    TableNumber = tableNumber,
                    TableKey = $"loc-1#{tableNumber}",
                    StartDateTime = "2026-03-05T10:00:00.0000000Z",
                    EndDateTime = "2026-03-05T11:30:00.0000000Z",
                    GuestsCount = 2,
                    Status = ReservationStatus.Reserved,
                    CreatedAt = "2026-03-01T00:00:00.0000000Z",
                    UpdatedAt = "2026-03-01T00:00:00.0000000Z"
                });
            }
        }

        private sealed class FakeLocationService : ILocationService
        {
            public Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Location>>(new List<Location>
                {
                new()
                {
                    Id = "loc-1",
                    Address = "Main street 1",
                    Description = "Test",
                    TotalCapacity = 120,
                    AverageOccupancy = 0.35,
                    ImageUrl = "http://img",
                    Rating = 4.6
                }
                });

            public Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
                => GetLocationsAsync(cancellationToken);
        }

        private sealed class FakeFeedbackService : IFeedbackService
        {
            public Task<Core.ServiceDTOs.FeedbackPaginatedDto> GetFeedbacksForLocation(
                string locationId, int size, string type, List<string> sort, string? pageToken)
                => Task.FromResult(new Core.ServiceDTOs.FeedbackPaginatedDto
                {
                    Size = size,
                    Content = new List<Core.ServiceDTOs.FeedbackDTO>(),
                    NextPageToken = null
                });
        }

        private sealed class FakeDishService : IDishService
        {
            public Task<IReadOnlyList<Dish>> GetSpecialityDishesByLocationIdAsync(
                string locationId, CancellationToken cancellationToken)
                => Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());

            public Task<IReadOnlyList<Dish>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Dish>>(Array.Empty<Dish>());
        }
    }
}
