using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

public class UserRepositoryIntegrationTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly UserRepository _repo;

    public UserRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo = new UserRepository(_context);
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

        await _repo.CreateAsync(user);

        var loaded = await _context.LoadAsync<User>(userId);

        loaded.Should().NotBeNull();
        loaded!.Role.Should().Be("CUSTOMER");
        loaded.FirstNameNormalized.Should().Be("john");
        loaded.LastNameNormalized.Should().Be("doe");
        loaded.EmailNormalized.Should().Be("john.doe.integration@test.com");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByFirstNamePrefix()
    {
        await CreateUserAsync("John", "Doe", "john.doe.first@test.com", "CUSTOMER");
        await CreateUserAsync("Jane", "Smith", "jane.smith.first@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync("jo");

        result.Should().ContainSingle(x => x.FirstName == "John" && x.LastName == "Doe");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByLastNamePrefix()
    {
        await CreateUserAsync("Alice", "Johnson", "alice.johnson.last@test.com", "CUSTOMER");
        await CreateUserAsync("Bob", "Brown", "bob.brown.last@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync("john");

        result.Should().ContainSingle(x => x.FirstName == "Alice" && x.LastName == "Johnson");
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldFindByEmailPrefix()
    {
        await CreateUserAsync("Chris", "Miller", "chris.lookup.email@test.com", "CUSTOMER");
        await CreateUserAsync("Diana", "Taylor", "diana.lookup.email@test.com", "CUSTOMER");

        var result = await _repo.SearchCustomersAsync("chris.lookup");

        result.Should().ContainSingle(x => x.FirstName == "Chris" && x.LastName == "Miller");
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
        var email = "john.unique.multi@test.com";
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

        await _repo.CreateAsync(user);
    }
}