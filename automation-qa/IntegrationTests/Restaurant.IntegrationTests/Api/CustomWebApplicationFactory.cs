using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Restaurant.Api;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
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
    public FakeOrderService OrderService { get; } = new();
    public FakeAuthService AuthService { get; } = new();
    public FakeCognitoService CognitoService { get; } = new();
    public FakeTableService TableService { get; } = new();

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

            services.RemoveAll<IOrderService>();
            services.AddSingleton<IOrderService>(OrderService);

            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(AuthService);

            services.RemoveAll<ICognitoService>();
            services.AddSingleton<ICognitoService>(CognitoService);

            services.RemoveAll<ITableService>();
            services.AddSingleton<ITableService>(TableService);
        });
    }

    public sealed class FakeAuthService : IAuthService
    {
        public AuthResult SignInResponse { get; set; } = new("id-token", "refresh-token", "John Doe", "CUSTOMER");

        public BusinessError? SignUpFailResult { get; set; }
        public BusinessError? SignInFailResult { get; set; }

        public string? LastSignUpEmail { get; private set; }
        public string? LastSignUpPassword { get; private set; }
        public string? LastSignUpFirstName { get; private set; }
        public string? LastSignUpLastName { get; private set; }
        public string? LastSignInEmail { get; private set; }
        public string? LastSignInPassword { get; private set; }

        public void Reset()
        {
            SignInResponse = new AuthResult("id-token", "refresh-token", "John Doe", "CUSTOMER");
            SignUpFailResult = null;
            SignInFailResult = null;
            LastSignUpEmail = null;
            LastSignUpPassword = null;
            LastSignUpFirstName = null;
            LastSignUpLastName = null;
            LastSignInEmail = null;
            LastSignInPassword = null;
        }

        public Task<Result> SignUpAsync(string email, string password, string firstName, string lastName, CancellationToken ct = default)
        {
            LastSignUpEmail = email;
            LastSignUpPassword = password;
            LastSignUpFirstName = firstName;
            LastSignUpLastName = lastName;

            if (SignUpFailResult is not null)
                return Task.FromResult(Result.Fail(SignUpFailResult));

            return Task.FromResult(Result.Ok());
        }

        public Task<Result<AuthResult>> SignInAsync(string email, string password, CancellationToken ct = default)
        {
            LastSignInEmail = email;
            LastSignInPassword = password;

            if (SignInFailResult is not null)
                return Task.FromResult(Result.Fail<AuthResult>(SignInFailResult));

            return Task.FromResult(Result.Ok(SignInResponse));
        }
    }

    public sealed class FakeOrderService : IOrderService
    {
        public string? LastActorId { get; private set; }
        public CreateOrderDTO? LastDto { get; private set; }
        public BusinessError? CreateFailResult { get; set; }
        public Order CreateResponse { get; set; } = BuildDefaultOrder();

        public void Reset()
        {
            LastActorId = null;
            LastDto = null;
            CreateFailResult = null;
            CreateResponse = BuildDefaultOrder();
        }

        public Task<Result<Order>> CreateAsyncForReservation(string actorId, CreateOrderDTO dto, CancellationToken ct = default)
        {
            LastActorId = actorId;
            LastDto = dto;

            if (CreateFailResult is not null)
                return Task.FromResult(Result.Fail<Order>(CreateFailResult));

            return Task.FromResult(Result.Ok(CreateResponse));
        }

        private static Order BuildDefaultOrder()
            => new()
            {
                Id = "o-1",
                ReservationId = "r-customer-1",
                LocationId = "loc-1",
                LocationAddress = "Main street 1",
                WaiterId = "waiter-1",
                WaiterName = "Walter One",
                CustomerId = "customer-1",
                CustomerName = "Anna Smith",
                VisitorName = null,
                TableNumber = 3,
                GuestsCount = 2,
                Status = OrderStatus.Open,
                Dishes = new List<OrderDishSnapshot>(),
                TotalAmount = 0,
                CreatedAt = "2026-03-01T00:00:00.0000000Z",
                CompletedAt = null
            };
    }

    public sealed class FakeCognitoService : ICognitoService
    {
        public string RefreshTokenResponse { get; set; } = "new-access-token";
        public BusinessError? RefreshTokenFailResult { get; set; }
        public BusinessError? SignOutFailResult { get; set; }
        public string? LastRefreshTokenInput { get; private set; }
        public string? LastSignOutRefreshToken { get; private set; }

        public void Reset()
        {
            RefreshTokenResponse = "new-access-token";
            RefreshTokenFailResult = null;
            SignOutFailResult = null;
            LastRefreshTokenInput = null;
            LastSignOutRefreshToken = null;
        }

        public string GetUserPoolId() => "test-pool";

        public Task<Result<string>> SignUpAsync(string email, string password, string firstName, string lastName, string role, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<Result<(string IdToken, string RefreshToken)>> SignInAsync(string email, string password, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<Result> DeleteUserAsync(string email, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            LastRefreshTokenInput = refreshToken;

            if (RefreshTokenFailResult is not null)
                return Task.FromResult(Result.Fail<string>(RefreshTokenFailResult));

            return Task.FromResult(Result.Ok(RefreshTokenResponse));
        }

        public Task<Result> SignOutAsync(string refreshToken, CancellationToken ct = default)
        {
            LastSignOutRefreshToken = refreshToken;

            if (SignOutFailResult is not null)
                return Task.FromResult(Result.Fail(SignOutFailResult));

            return Task.FromResult(Result.Ok());
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
        public List<WaiterCustomerLookupDTO> CustomerLookupResults { get; } = new();
        public string? LastSearchActorUserId { get; private set; }
        public string? LastSearchQuery { get; private set; }
        public string? LastCreateForWaiterActorUserId { get; private set; }
        public CreateReservationForWaiterDTO? LastCreateForWaiterDto { get; private set; }
        public string? LastLifecycleReservationId { get; private set; }
        public string? LastLifecycleWaiterId { get; private set; }
        public string? LastLifecycleAction { get; private set; }

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
            CustomerLookupResults.Clear();
            LastSearchActorUserId = null;
            LastSearchQuery = null;
            LastCreateForWaiterActorUserId = null;
            LastCreateForWaiterDto = null;
            LastLifecycleReservationId = null;
            LastLifecycleWaiterId = null;
            LastLifecycleAction = null;

            SeedReservations.Add(new Reservation
            {
                Id = "r-customer-1",
                CustomerId = "customer-1",
                CustomerName = "Anna Smith",
                WaiterId = "waiter-1",
                WaiterName = "Walter One",
                LocationId = "loc-1",
                LocationAddress = "Main street 1",
                TableNumber = 3,
                TableKey = "loc-1#3",
                StartDateTime = "2026-03-05T10:00:00.0000000Z",
                EndDateTime = "2026-03-05T11:30:00.0000000Z",
                ActualStartTime = null,
                ActualEndTime = null,
                GuestsCount = 2,
                DishCount = 0,
                Status = ReservationStatus.Reserved,
                IsCreatedByWaiter = false,
                VisitorName = null,
                CreatedAt = "2026-03-01T00:00:00.0000000Z",
                UpdatedAt = "2026-03-01T00:00:00.0000000Z"
            });

            SeedReservations.Add(new Reservation
            {
                Id = "r-customer-2",
                CustomerId = "customer-2",
                CustomerName = "John Doe",
                WaiterId = "waiter-1",
                WaiterName = "Walter One",
                LocationId = "loc-2",
                LocationAddress = "Second street 2",
                TableNumber = 7,
                TableKey = "loc-2#7",
                StartDateTime = "2026-03-06T12:00:00.0000000Z",
                EndDateTime = "2026-03-06T13:30:00.0000000Z",
                ActualStartTime = null,
                ActualEndTime = null,
                GuestsCount = 4,
                DishCount = 3,
                Status = ReservationStatus.Reserved,
                IsCreatedByWaiter = false,
                VisitorName = null,
                CreatedAt = "2026-03-02T00:00:00.0000000Z",
                UpdatedAt = "2026-03-02T00:00:00.0000000Z"
            });

            CustomerLookupResults.Add(new WaiterCustomerLookupDTO("customer-1", "Anna Smith", "a**a@example.com"));
            CustomerLookupResults.Add(new WaiterCustomerLookupDTO("customer-2", "John Doe", "j******e@example.com"));
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

        public Task<Result<Reservation>> GetByIdAsync(string id, string actorUserId, bool actorIsWaiter, CancellationToken ct = default)
        {
            var entity = SeedReservations.SingleOrDefault(x => x.Id == id);
            if (entity is null)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.ReservationNotFound));

            var allowed = entity.CustomerId == actorUserId
                          || (actorIsWaiter && entity.WaiterId == actorUserId);

            if (!allowed)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.Forbidden));

            return Task.FromResult(Result.Ok(entity));
        }

        public Task<Result> CancelReservation(string reservationId, string userId, bool isWaiter, CancellationToken ct = default)
        {
            LastCancelReservationId = reservationId;
            LastCancelUserId = userId;
            LastCancelIsWaiter = isWaiter;

            var entity = SeedReservations.SingleOrDefault(x => x.Id == reservationId);
            if (entity is null)
                return Task.FromResult(Result.Fail(ReservationErrors.ReservationNotFound));

            var allowed = entity.CustomerId == userId || (isWaiter && entity.WaiterId == userId);
            if (!allowed)
                return Task.FromResult(Result.Fail(ReservationErrors.Forbidden));

            if (!CancelShouldSucceed)
                return Task.FromResult(Result.Fail(ReservationErrors.CancellationFailed));

            entity.Status = ReservationStatus.Cancelled;
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
            return Task.FromResult(Result.Ok());
        }

        public Task<Result<Reservation>> CreateForClientAsync(string customerId, CreateReservationDTO dto, CancellationToken ct = default)
        {
            LastCreateCustomerId = customerId;
            LastCreateDto = dto;

            var start = dto.Date.ToDateTime(dto.TimeFrom, DateTimeKind.Utc);
            var end = dto.Date.ToDateTime(dto.TimeTo, DateTimeKind.Utc);

            var created = new Reservation
            {
                Id = "r-created-1",
                CustomerId = customerId,
                CustomerName = $"Customer {customerId}",
                WaiterId = "waiter-auto",
                WaiterName = "Auto Waiter",
                LocationId = dto.LocationId,
                LocationAddress = "Generated address",
                TableNumber = dto.TableNumber,
                TableKey = $"{dto.LocationId}#{dto.TableNumber}",
                StartDateTime = start.ToString("O"),
                EndDateTime = end.ToString("O"),
                ActualStartTime = null,
                ActualEndTime = null,
                GuestsCount = dto.GuestsCount,
                DishCount = 0,
                Status = ReservationStatus.Reserved,
                IsCreatedByWaiter = false,
                VisitorName = null,
                CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
            };

            SeedReservations.Add(created);
            return Task.FromResult(Result.Ok(created));
        }

        public Task<Result<Reservation>> CreateForWaiterAsync(string waiterId, CreateReservationForWaiterDTO dto, CancellationToken ct = default)
        {
            LastCreateForWaiterActorUserId = waiterId;
            LastCreateForWaiterDto = dto;

            var start = new DateTimeOffset(dto.Date.ToDateTime(dto.TimeFrom), TimeSpan.Zero);
            var endDate = dto.TimeTo < dto.TimeFrom ? dto.Date.AddDays(1) : dto.Date;
            var end = new DateTimeOffset(endDate.ToDateTime(dto.TimeTo), TimeSpan.Zero);

            var reservation = new Reservation
            {
                Id = $"r-waiter-created-{SeedReservations.Count + 1}",
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerId is null ? null : $"Customer {dto.CustomerId}",
                WaiterId = waiterId,
                WaiterName = $"Waiter {waiterId}",
                LocationId = dto.LocationId,
                LocationAddress = "Main street 1",
                TableNumber = dto.TableNumber,
                TableKey = $"{dto.LocationId}#{dto.TableNumber}",
                StartDateTime = start.ToString("O"),
                EndDateTime = end.ToString("O"),
                ActualStartTime = null,
                ActualEndTime = null,
                GuestsCount = dto.GuestsCount,
                DishCount = 0,
                Status = ReservationStatus.Reserved,
                IsCreatedByWaiter = true,
                VisitorName = dto.VisitorName,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            SeedReservations.Add(reservation);

            return Task.FromResult(Result.Ok(reservation));
        }

        public Task<Result<IReadOnlyList<WaiterCustomerLookupDTO>>> SearchCustomersForWaiterAsync(string actorUserId, string query, CancellationToken ct = default)
        {
            LastSearchActorUserId = actorUserId;
            LastSearchQuery = query;

            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(Result.Ok<IReadOnlyList<WaiterCustomerLookupDTO>>(Array.Empty<WaiterCustomerLookupDTO>()));

            var result = CustomerLookupResults
                .Where(x => x.Username.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || x.MaskedEmail.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(Result.Ok<IReadOnlyList<WaiterCustomerLookupDTO>>(result));
        }

        public Task<Result<Reservation>> UpdateReservationAsync(string actorUserId, bool isActorWaiter, UpdateReservationDTO dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<Reservation>> StartReservationAsync(string reservationId, string waiterId, CancellationToken ct = default)
        {
            LastLifecycleReservationId = reservationId;
            LastLifecycleWaiterId = waiterId;
            LastLifecycleAction = "start";

            var entity = SeedReservations.SingleOrDefault(x => x.Id == reservationId);
            if (entity is null)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.ReservationNotFound));

            if (entity.WaiterId != waiterId)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.Forbidden));

            entity.Status = ReservationStatus.InProgress;
            entity.ActualStartTime = DateTimeOffset.UtcNow.ToString("O");
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
            return Task.FromResult(Result.Ok(entity));
        }

        public Task<Result<Reservation>> MarkMealsServedAsync(string reservationId, string waiterId, CancellationToken ct = default)
        {
            LastLifecycleReservationId = reservationId;
            LastLifecycleWaiterId = waiterId;
            LastLifecycleAction = "meals-served";

            var entity = SeedReservations.SingleOrDefault(x => x.Id == reservationId);
            if (entity is null)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.ReservationNotFound));

            if (entity.WaiterId != waiterId)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.Forbidden));

            entity.Status = ReservationStatus.MealsServed;
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
            return Task.FromResult(Result.Ok(entity));
        }

        public Task<Result<Reservation>> FinishReservationAsync(string reservationId, string waiterId, CancellationToken ct = default)
        {
            LastLifecycleReservationId = reservationId;
            LastLifecycleWaiterId = waiterId;
            LastLifecycleAction = "finish";

            var entity = SeedReservations.SingleOrDefault(x => x.Id == reservationId);
            if (entity is null)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.ReservationNotFound));

            if (entity.WaiterId != waiterId)
                return Task.FromResult(Result.Fail<Reservation>(ReservationErrors.Forbidden));

            entity.Status = ReservationStatus.Finished;
            entity.ActualEndTime = DateTimeOffset.UtcNow.ToString("O");
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
            return Task.FromResult(Result.Ok(entity));
        }
    }

    public sealed class FakeLocationService : ILocationService
    {
        public List<Location> Locations { get; } = new();
        public List<Location> Options { get; } = new();
        public string? LastGetByIdId { get; private set; }

        public FakeLocationService()
        {
            Reset();
        }

        public void Reset()
        {
            Locations.Clear();
            Options.Clear();
            LastGetByIdId = null;

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

        public Task<Result<IReadOnlyList<Location>>> GetLocationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok<IReadOnlyList<Location>>(Locations.ToList()));

        public Task<Result<IReadOnlyList<Location>>> GetLocationOptionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok<IReadOnlyList<Location>>(Options.ToList()));

        public Task<Result<Location>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            LastGetByIdId = id;
            var location = Locations.SingleOrDefault(x => x.Id == id);
            if (location is null)
                return Task.FromResult(Result.Fail<Location>(LocationErrors.NotFound));
            return Task.FromResult(Result.Ok(location));
        }
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

        public Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(
            string locationId,
            int size,
            string type,
            List<string> sort,
            string? pageToken,
            CancellationToken ct = default)
        {
            LastLocationId = locationId;
            LastSize = size;
            LastType = type;
            LastSort = sort.ToList();
            LastPageToken = pageToken;

            return Task.FromResult(Result.Ok(Response));
        }
    }

    public sealed class FakeDishService : IDishService
    {
        public string? LastLocationId { get; private set; }
        public string? LastDishId { get; private set; }
        public string? LastMenuType { get; private set; }
        public string? LastMenuSort { get; private set; }
        public Dictionary<string, IReadOnlyList<Dish>> DishesByLocation { get; } = new();
        public List<Dish> PopularDishes { get; } = new();
        public Dictionary<string, Dish> DishesById { get; } = new();
        public List<DishBriefDTO> MenuDishes { get; } = new();

        public FakeDishService()
        {
            Reset();
        }

        public void Reset()
        {
            LastLocationId = null;
            LastDishId = null;
            LastMenuType = null;
            LastMenuSort = null;

            DishesByLocation.Clear();
            DishesByLocation["loc-1"] = Array.Empty<Dish>();

            PopularDishes.Clear();
            DishesById.Clear();
            MenuDishes.Clear();
        }

        public Task<Result<IReadOnlyList<Dish>>> GetSpecialityDishesByLocationIdAsync(
            string locationId,
            CancellationToken cancellationToken = default)
        {
            LastLocationId = locationId;

            if (DishesByLocation.TryGetValue(locationId, out var dishes))
                return Task.FromResult(Result.Ok(dishes));

            return Task.FromResult(Result.Ok<IReadOnlyList<Dish>>(Array.Empty<Dish>()));
        }

        public Task<Result<IReadOnlyList<Dish>>> GetPopularDishesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok<IReadOnlyList<Dish>>(PopularDishes));

        public Task<Result<Dish>> GetDishByIdAsync(string dishId,
            CancellationToken cancellationToken = default)
        {
            LastDishId = dishId;
            if (DishesById.TryGetValue(dishId, out var dish))
                return Task.FromResult(Result.Ok(dish));

            return Task.FromResult(Result.Fail<Dish>(DishErrors.NotFound));
        }

        public Task<Result<IReadOnlyList<DishBriefDTO>>> GetMenuBriefDishesAsync(
            string? type, string sort, CancellationToken cancellationToken = default)
        {
            LastMenuType = type;
            LastMenuSort = sort;
            return Task.FromResult(Result.Ok<IReadOnlyList<DishBriefDTO>>(MenuDishes));
        }
    }

    public sealed class FakeTableService : ITableService
    {
        public List<TableWithAvailableSlots> Response { get; } = new();
        public BusinessError? FailResult { get; set; }

        public DateOnly? LastDate { get; private set; }
        public TimeOnly? LastTime { get; private set; }
        public string? LastLocationId { get; private set; }
        public int? LastCapacity { get; private set; }

        public FakeTableService()
        {
            Reset();
        }

        public void Reset()
        {
            Response.Clear();
            FailResult = null;
            LastDate = null;
            LastTime = null;
            LastLocationId = null;
            LastCapacity = null;

            Response.Add(new TableWithAvailableSlots
            {
                LocationId = "loc-1",
                TableNumber = 3,
                LocationAddress = "Main street 1",
                Capacity = 4,
                AvailableSlots = new List<TimeSlot>
                {
                    new() { StartOffset = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero), EndOffset = new DateTimeOffset(2026, 6, 1, 10, 15, 0, TimeSpan.Zero) },
                    new() { StartOffset = new DateTimeOffset(2026, 6, 1, 10, 15, 0, TimeSpan.Zero), EndOffset = new DateTimeOffset(2026, 6, 1, 10, 30, 0, TimeSpan.Zero) }
                }
            });
        }

        public Task<Result<IList<TableWithAvailableSlots>>> GetAvailableTablesAsync(
            DateOnly date,
            TimeOnly? time,
            string? locationId,
            int? capacity,
            CancellationToken ct)
        {
            LastDate = date;
            LastTime = time;
            LastLocationId = locationId;
            LastCapacity = capacity;

            if (FailResult is not null)
                return Task.FromResult(Result.Fail<IList<TableWithAvailableSlots>>(FailResult));

            return Task.FromResult(Result.Ok<IList<TableWithAvailableSlots>>(Response.ToList()));
        }
    }
}
