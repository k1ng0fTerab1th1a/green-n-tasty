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
    public async Task CreateAsync_ShouldPersistUser()
    {
        var userId = Guid.NewGuid().ToString("N");

        var user = new User
        {
            UserId = userId,
            Email = "integration@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "CUSTOMER",
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _repo.CreateAsync(user);

        var loaded = await _context.LoadAsync<User>(userId);

        loaded.Should().NotBeNull();
    }
}
