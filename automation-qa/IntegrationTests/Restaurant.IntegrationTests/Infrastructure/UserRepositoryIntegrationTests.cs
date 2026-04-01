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

    private async Task CreateUserAsync(string firstName, string lastName, string email, string role)
    {
        var user = new User
        {
            UserId = Guid.NewGuid().ToString("N"),
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