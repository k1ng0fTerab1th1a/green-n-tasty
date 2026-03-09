using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.Infrastructure.IntegrationTests;

public sealed class LocationRepositoryIntegrationTests
{
    private static Task<IAmazonDynamoDB> CreateClientAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:8000";

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = endpoint,
            UseHttp = true
        };

        IAmazonDynamoDB client = new AmazonDynamoDBClient(new BasicAWSCredentials("test", "test"), config);
        return Task.FromResult(client);
    }

    private static async Task EnsureLocationsTableAsync(IAmazonDynamoDB client)
    {
        const string tableName = "Locations";

        var existing = await client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
            return;

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("id", ScalarAttributeType.S),
                new("entityType", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new("id", KeyType.HASH)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new()
                {
                    IndexName = "entityType-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("entityType", KeyType.HASH),
                        new("id", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            }
        };

        await client.CreateTableAsync(request);

        while (true)
        {
            var desc = await client.DescribeTableAsync(tableName);
            if (desc.Table.TableStatus == TableStatus.ACTIVE)
                break;

            await Task.Delay(500);
        }
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldReturnInsertedLocation()
    {
        using var client = await CreateClientAsync();
        await EnsureLocationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new LocationRepository(ctx);

        var id = Guid.NewGuid().ToString("N");

        await ctx.SaveAsync(new Location
        {
            Id = id,
            EntityType = "LOCATION",
            Address = "Berlin, Test str 1",
            Description = "Test",
            TotalCapacity = 10,
            AverageOccupancy = 0.5,
            ImageUrl = "http://img",
            Rating = 4.2
        });

        var items = await repo.GetLocationsAsync();

        items.Should().Contain(x => x.Id == id);
    }

    [Fact]
    public async Task GetLocationOptionsAsync_ShouldReturnInsertedLocation()
    {
        using var client = await CreateClientAsync();
        await EnsureLocationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new LocationRepository(ctx);

        var id = Guid.NewGuid().ToString("N");

        await ctx.SaveAsync(new Location
        {
            Id = id,
            EntityType = "LOCATION",
            Address = "Hamburg, Test str 2",
            Description = "Test",
            TotalCapacity = 20,
            AverageOccupancy = 0.7,
            ImageUrl = "http://img2",
            Rating = 4.8
        });

        var items = await repo.GetLocationOptionsAsync();

        items.Should().Contain(x => x.Id == id && x.Address == "Hamburg, Test str 2");
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldReturnOnlyLocationEntities()
    {
        using var client = await CreateClientAsync();
        await EnsureLocationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new LocationRepository(ctx);

        var locationId = Guid.NewGuid().ToString("N");
        var otherEntityId = Guid.NewGuid().ToString("N");

        await ctx.SaveAsync(new Location
        {
            Id = locationId,
            EntityType = "LOCATION",
            Address = "Real location",
            Description = "Visible",
            TotalCapacity = 30,
            AverageOccupancy = 0.3,
            ImageUrl = "http://img/location",
            Rating = 4.0
        });

        await ctx.SaveAsync(new Location
        {
            Id = otherEntityId,
            EntityType = "NOT_LOCATION",
            Address = "Should be filtered out",
            Description = "Hidden",
            TotalCapacity = 999,
            AverageOccupancy = 1.0,
            ImageUrl = "http://img/other",
            Rating = 1.0
        });

        var items = await repo.GetLocationsAsync();

        items.Should().Contain(x => x.Id == locationId);
        items.Should().NotContain(x => x.Id == otherEntityId);
    }
}
