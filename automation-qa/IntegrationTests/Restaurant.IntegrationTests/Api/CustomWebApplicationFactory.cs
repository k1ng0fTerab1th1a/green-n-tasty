using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Restaurant.Api;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.SharedModels;

namespace Restaurant.IntegrationTests.Api;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeReservationService ReservationService { get; } = new();
    public FakeLocationService LocationService { get; } = new();
    public FakeFeedbackService FeedbackService { get; } = new();
    public FakeDishService DishService { get; } = new();
    public FakeAuthService AuthService { get; } = new();
    public FakeCognitoService CognitoService { get; } = new();

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

            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(AuthService);

            services.RemoveAll<ICognitoService>();
            services.AddSingleton<ICognitoService>(CognitoService);
        });
    }

    public sealed class FakeAuthService : IAuthService
    {
        public AuthResult SignInResponse { get; set; } = new("id-token", "refresh-token", "John Doe", "CUSTOMER");

        public Exception? SignUpException { get; set; }
        public Exception? SignInException { get; set; }

        public string? LastSignUpEmail { get; private set; }
        public string? LastSignUpPassword { get; private set; }
        public string? LastSignUpFirstName { get; private set; }
        public string? LastSignUpLastName { get; private set; }
        public string? LastSignInEmail { get; private set; }
        public string? LastSignInPassword { get; private set; }

        public void Reset()
        {
            SignInResponse = new AuthResult("id-token", "refresh-token", "John Doe", "CUSTOMER");
            SignUpException = null;
            SignInException = null;
            LastSignUpEmail = null;
            LastSignUpPassword = null;
            LastSignUpFirstName = null;
            LastSignUpLastName = null;
            LastSignInEmail = null;
            LastSignInPassword = null;
        }

        public Task SignUpAsync(string email, string password, string firstName, string lastName)
        {
            LastSignUpEmail = email;
            LastSignUpPassword = password;
            LastSignUpFirstName = firstName;
            LastSignUpLastName = lastName;

            if (SignUpException is not null)
                throw SignUpException;

            return Task.CompletedTask;
        }

        public Task<AuthResult> SignInAsync(string email, string password)
        {
            LastSignInEmail = email;
            LastSignInPassword = password;

            if (SignInException is not null)
                throw SignInException;

            return Task.FromResult(SignInResponse);
        }
    }

    public sealed class FakeCognitoService : ICognitoService
    {
        public string RefreshTokenResponse { get; set; } = "new-access-token";
        public Exception? RefreshTokenException { get; set; }
        public Exception? SignOutException { get; set; }
        public string? LastRefreshTokenInput { get; private set; }
        public string? LastSignOutRefreshToken { get; private set; }

        public void Reset()
        {
            RefreshTokenResponse = "new-access-token";
            RefreshTokenException = null;
            SignOutException = null;
            LastRefreshTokenInput = null;
            LastSignOutRefreshToken = null;
        }

        public string GetUserPoolId() => "test-pool";

        public Task<string> SignUpAsync(string email, string password, string firstName, string lastName, string role = "CUSTOMER")
            => throw new NotImplementedException();

        public Task<(string IdToken, string RefreshToken)> SignInAsync(string email, string password)
            => throw new NotImplementedException();

        public Task DeleteUserAsync(string email)
            => throw new NotImplementedException();

        public Task<string> RefreshTokenAsync(string refreshToken)
        {
            LastRefreshTokenInput = refreshToken;

            if (RefreshTokenException is not null)
                throw RefreshTokenException;

            return Task.FromResult(RefreshTokenResponse);
        }

        public Task SignOutAsync(string refreshToken)
        {
            LastSignOutRefreshToken = refreshToken;

            if (SignOutException is not null)
                throw SignOutException;

            return Task.CompletedTask;
        }
    }

    public sealed class FakeReservationService : IReservationService
    {
        public List<Reservation> SeedReservations { get; } = new();
        public string? LastCreateCustomerId { get; private set; }
        public CreateReservationDTO? LastCreateDto { get; private set; }
        public string? LastCancelReservationId { get; private set; }
        public string? LastCancelUserId { get; private set; }
        public bool? LastCancelIsWaiter { get; private set; }
        public bool CancelShouldSucceed { get; set; }

        public FakeReservationService()
        {
            Reset();
        }

        public void Reset()
        {
            SeedReservations.Clear();
            LastCreateCustomerId = null;
            LastCreateDto = null;
            LastCancelReservationId = null;
            LastCancelUserId = null;
            LastCancelIsWaiter = null;
            CancelShouldSucceed = true;

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
            LastCancelReservationId = reservationId;
            LastCancelUserId = userId;
            LastCancelIsWaiter = isWaiter;

            var entity = SeedReservations.SingleOrDefault(x => x.Id == reservationId);
            if (entity is null)
                return Task.FromResult(false);

            var allowed = entity.CustomerId == userId || (isWaiter && entity.WaiterId == userId);
            if (!allowed)
                throw new UnauthorizedAccessException("Forbidden.");

            if (!CancelShouldSucceed)
                return Task.FromResult(false);

            entity.Status = ReservationStatus.Cancelled;
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
            return Task.FromResult(true);
        }

        public Task<Reservation> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
        {
            LastCreateCustomerId = customerId;
            LastCreateDto = dto;

            var start = dto.Date.ToDateTime(dto.TimeFrom, DateTimeKind.Utc);
            var end = dto.Date.ToDateTime(dto.TimeTo, DateTimeKind.Utc);

            var created = new Reservation
            {
                Id = "r-created-1",
                CustomerId = customerId,
                WaiterId = "waiter-auto",
                LocationId = dto.LocationId,
                LocationAddress = "Generated address",
                TableNumber = dto.TableNumber,
                TableKey = $"{dto.LocationId}#{dto.TableNumber}",
                StartDateTime = start.ToString("O"),
                EndDateTime = end.ToString("O"),
                GuestsCount = dto.GuestsCount,
                Status = ReservationStatus.Reserved,
                CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
            };

            SeedReservations.Add(created);
            return Task.FromResult(created);
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
