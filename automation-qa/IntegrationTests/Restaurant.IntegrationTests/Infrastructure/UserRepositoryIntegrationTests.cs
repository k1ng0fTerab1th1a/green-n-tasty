using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public class UserRepositoryIntegrationTests
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;
    private readonly UserRepository _repo;

    public UserRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _client = fixture.Client;
        _repo = new UserRepository(_context, _client);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistUser_AndPopulateNormalizedFields()
    {
        var userId = Guid.NewGuid().ToString("N");

        var user = new User
        {
            UserId = userId,
            Email = "john.doe.integration@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "customer",
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        CancellationToken ct = CancellationToken.None;
        await _repo.CreateAsync(user, ct);

        var loaded = await _context.LoadAsync<User>(userId, ct);

        loaded.Should().NotBeNull();
        loaded!.Role.Should().Be("CUSTOMER");
        loaded.FirstNameNormalized.Should().Be("john");
        loaded.LastNameNormalized.Should().Be("doe");
        loaded.EmailNormalized.Should().Be("john.doe.integration@test.com");
        loaded.WaiterFlag.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistWaiterFlag_WhenSet()
    {
        var userId = Guid.NewGuid().ToString("N");

        var user = new User
        {
            UserId = userId,
            Email = "waiter.integration@test.com",
            FirstName = "Wait",
            LastName = "Er",
            Role = "WAITER",
            WaiterFlag = "1",
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        CancellationToken ct = CancellationToken.None;
        await _repo.CreateAsync(user, ct);

        var loaded = await _context.LoadAsync<User>(userId, ct);

        loaded.Should().NotBeNull();
        loaded!.Role.Should().Be("WAITER");
        loaded.WaiterFlag.Should().Be("1");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByFirstNamePrefix()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        await CreateUserAsync($"John{suffix}", $"Doe{suffix}", $"john.{suffix}@test.com", "CUSTOMER");
        await CreateUserAsync($"Jane{suffix}", $"Smith{suffix}", $"jane.{suffix}@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync($"john{suffix[..3]}");

        result.Should().ContainSingle(x => x.FirstName == $"John{suffix}" && x.LastName == $"Doe{suffix}");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByLastNamePrefix()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        await CreateUserAsync($"Alice{suffix}", $"Johnson{suffix}", $"alice.{suffix}@test.com", "CUSTOMER");
        await CreateUserAsync($"Bob{suffix}", $"Brown{suffix}", $"bob.{suffix}@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync($"johnson{suffix[..3]}");

        result.Should().ContainSingle(x => x.FirstName == $"Alice{suffix}" && x.LastName == $"Johnson{suffix}");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByEmailPrefix()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        await CreateUserAsync($"Alice{suffix}", $"Johnson{suffix}", $"alice.{suffix}@test.com", "CUSTOMER");
        await CreateUserAsync($"Bob{suffix}", $"Brown{suffix}", $"bob.{suffix}@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync($"johnson{suffix[..3]}");

        result.Should().ContainSingle(x => x.FirstName == $"Alice{suffix}" && x.LastName == $"Johnson{suffix}");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldReturnOnlyCustomers()
    {
        await CreateUserAsync("John", "Customer", "john.customer.role@test.com", "CUSTOMER");
        await CreateUserAsync("John", "Waiter", "john.waiter.role@test.com", "WAITER");

        var result = await _repo.SearchCustomersAsync("john");

        result.Should().Contain(x => x.LastName == "Customer");
        result.Should().NotContain(x => x.LastName == "Waiter");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldDeduplicateUserFoundInMultipleIndexes()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"john.unique.multi.{suffix}@test.com";
        await CreateUserAsync("John", "Unique", email, "CUSTOMER");

        var result = await _repo.SearchCustomersAsync("john");

        result.Count(x => x.Email == email).Should().Be(1);
    }
    

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        var userId = Guid.NewGuid().ToString("N");
        await CreateUserAsync("Get", "ById", $"getbyid@test.com", "CUSTOMER", userId);

        var result = await _repo.GetByIdAsync(userId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.FirstName.Should().Be("Get");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid().ToString("N"));

        result.Should().BeNull();
    }


    [Fact]
    public async Task GetUserDataForFeedbackCreationByIdAsync_WhenUserExists_ShouldReturnUsernameAndImageUrl()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId    = userId,
            FirstName = "Alice",
            LastName  = "Walker",
            Email     = $"alice.{userId}@test.com",
            Role      = "CUSTOMER",
            ImageUrl  = "https://cdn.test/avatar-alice.jpg",
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        var (username, imageUrl) = await _repo.GetUserDataForFeedbackCreationByIdAsync(userId);

        username.Should().Be("Alice Walker");
        imageUrl.Should().Be("https://cdn.test/avatar-alice.jpg");
    }

    [Fact]
    public async Task GetUserDataForFeedbackCreationByIdAsync_WhenUserHasNoImageUrl_ShouldReturnNullImageUrl()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId    = userId,
            FirstName = "Bob",
            LastName  = "Noimage",
            Email     = $"bob.{userId}@test.com",
            Role      = "CUSTOMER",
            ImageUrl  = null,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        var (username, imageUrl) = await _repo.GetUserDataForFeedbackCreationByIdAsync(userId);

        username.Should().Be("Bob Noimage");
        imageUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetUserDataForFeedbackCreationByIdAsync_WhenUserDoesNotExist_ShouldReturnEmptyUsername()
    {
        var (username, imageUrl) = await _repo.GetUserDataForFeedbackCreationByIdAsync(
            Guid.NewGuid().ToString("N"));

        username.Should().BeEmpty();
        imageUrl.Should().BeNull();
    }


    [Fact]
    public async Task UpdateUserRatingAsync_ShouldIncrementRatingAndFeedbacksCount()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId           = userId,
            FirstName        = "Rate",
            LastName         = "Me",
            Email            = $"rateme.{userId}@test.com",
            Role             = "WAITER",
            TotalRating      = 10,
            FeedbacksCount   = 2,
            CreatedAt        = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt        = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        await _repo.UpdateUserRatingAsync(userId, 5, CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.TotalRating.Should().Be(15);
        loaded.FeedbacksCount.Should().Be(3);
    }

    [Fact]
    public async Task UpdateUserRatingAsync_CalledMultipleTimes_ShouldAccumulateCorrectly()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId         = userId,
            FirstName      = "Multi",
            LastName       = "Rate",
            Email          = $"multirate.{userId}@test.com",
            Role           = "WAITER",
            TotalRating    = 0,
            FeedbacksCount = 0,
            CreatedAt      = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt      = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        await _repo.UpdateUserRatingAsync(userId, 4, CancellationToken.None);
        await _repo.UpdateUserRatingAsync(userId, 5, CancellationToken.None);
        await _repo.UpdateUserRatingAsync(userId, 3, CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.TotalRating.Should().Be(12);
        loaded.FeedbacksCount.Should().Be(3);
    }


    [Fact]
    public async Task GetWaiterFeedbackDataAsync_ShouldReturnCorrectData()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId         = userId,
            FirstName      = "Walter",
            LastName       = "White",
            Email          = $"walter.{userId}@test.com",
            Role           = "WAITER",
            ImageUrl       = "https://cdn.test/walter.jpg",
            TotalRating    = 42,
            FeedbacksCount = 7,
            CreatedAt      = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt      = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        var data = await _repo.GetWaiterFeedbackDataAsync(userId);

        data.WaiterName.Should().Be("Walter White");
        data.WaiterImageUrl.Should().Be("https://cdn.test/walter.jpg");
        data.WaiterRating.Should().Be(42);
        data.WaiterFeedbacksNumber.Should().Be(7);
    }

    [Fact]
    public async Task GetWaiterFeedbackDataAsync_WhenWaiterHasNoImageUrl_ShouldReturnNullImageUrl()
    {
        var userId = Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId         = userId,
            FirstName      = "Plain",
            LastName       = "Waiter",
            Email          = $"plain.{userId}@test.com",
            Role           = "WAITER",
            ImageUrl       = null,
            TotalRating    = 5,
            FeedbacksCount = 1,
            CreatedAt      = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt      = DateTimeOffset.UtcNow.ToString("O")
        };
        await _repo.CreateAsync(user, CancellationToken.None);

        var data = await _repo.GetWaiterFeedbackDataAsync(userId);

        data.WaiterImageUrl.Should().BeNull();
        data.WaiterName.Should().Be("Plain Waiter");
    }

    [Fact]
    public async Task GetWaiterFeedbackDataAsync_WhenUserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var act = () => _repo.GetWaiterFeedbackDataAsync(Guid.NewGuid().ToString("N"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }


    [Fact]
    public async Task UpdateEmailAsync_ShouldPersistNewEmail_AndUpdateNormalizedEmail()
    {
        var userId = Guid.NewGuid().ToString("N");
        await CreateUserAsync("Email", "Update", $"old.{userId}@test.com", "CUSTOMER", userId);

        await _repo.UpdateEmailAsync(userId, "NEW.Address@Test.COM", CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.Email.Should().Be("NEW.Address@Test.COM");
        loaded.EmailNormalized.Should().Be("new.address@test.com");
    }

    [Fact]
    public async Task UpdateEmailAsync_WhenUserDoesNotExist_ShouldNotThrow()
    {
        var act = () => _repo.UpdateEmailAsync(
            Guid.NewGuid().ToString("N"), "ghost@test.com", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }


    [Fact]
    public async Task UpdateUserNameAsync_ShouldPersistNewFirstAndLastName()
    {
        var userId = Guid.NewGuid().ToString("N");
        await CreateUserAsync("OldFirst", "OldLast", $"name.{userId}@test.com", "CUSTOMER", userId);

        await _repo.UpdateUserNameAsync(userId, "NewFirst", "NewLast", CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.FirstName.Should().Be("NewFirst");
        loaded.LastName.Should().Be("NewLast");
    }

    [Fact]
    public async Task UpdateUserNameAsync_WhenUserDoesNotExist_ShouldNotThrow()
    {
        var act = () => _repo.UpdateUserNameAsync(
            Guid.NewGuid().ToString("N"), "Ghost", "User", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }


    [Fact]
    public async Task UpdateAvatarUrlAsync_ShouldPersistNewUrl()
    {
        var userId = Guid.NewGuid().ToString("N");
        await CreateUserAsync("Avatar", "User", $"avatar.{userId}@test.com", "CUSTOMER", userId);

        await _repo.UpdateAvatarUrlAsync(userId, "https://cdn.test/new-avatar.jpg", CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.ImageUrl.Should().Be("https://cdn.test/new-avatar.jpg");
    }

    [Fact]
    public async Task UpdateAvatarUrlAsync_CalledTwice_ShouldRetainLatestUrl()
    {
        var userId = Guid.NewGuid().ToString("N");
        await CreateUserAsync("Avatar", "Twice", $"avatartwice.{userId}@test.com", "CUSTOMER", userId);

        await _repo.UpdateAvatarUrlAsync(userId, "https://cdn.test/v1.jpg", CancellationToken.None);
        await _repo.UpdateAvatarUrlAsync(userId, "https://cdn.test/v2.jpg", CancellationToken.None);

        var loaded = await _context.LoadAsync<User>(userId, CancellationToken.None);
        loaded!.ImageUrl.Should().Be("https://cdn.test/v2.jpg");
    }

    [Fact]
    public async Task UpdateAvatarUrlAsync_WhenUserDoesNotExist_ShouldThrow()
    {
        var act = () => _repo.UpdateAvatarUrlAsync(
            Guid.NewGuid().ToString("N"), "https://cdn.test/ghost.jpg", CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    private async Task CreateUserAsync(string firstName, string lastName, string email, string role, string userId = null)
    {
        var user = new User
        {
            UserId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString("N") : userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };
        CancellationToken ct = CancellationToken.None;

        await _repo.CreateAsync(user, ct);
    }
}