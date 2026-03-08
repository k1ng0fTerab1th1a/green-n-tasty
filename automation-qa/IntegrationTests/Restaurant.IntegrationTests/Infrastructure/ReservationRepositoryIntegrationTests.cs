using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.Infrastructure.IntegrationTests;

public sealed class ReservationRepositoryIntegrationTests
{
    private static async Task<IAmazonDynamoDB> CreateClientAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:8000";

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = endpoint,
            UseHttp = true
        };

        return new AmazonDynamoDBClient(new BasicAWSCredentials("test", "test"), config);
    }

    private static async Task EnsureReservationsTableAsync(IAmazonDynamoDB client)
    {
        var tableName = "Reservations";

        var existing = await client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
            return;

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("id", ScalarAttributeType.S),
                new("customerId", ScalarAttributeType.S),
                new("waiterId", ScalarAttributeType.S),
                new("tableKey", ScalarAttributeType.S),
                new("startDateTime", ScalarAttributeType.S)
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
                    IndexName = "customerId-start-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("customerId", KeyType.HASH),
                        new("startDateTime", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new()
                {
                    IndexName = "waiterId-start-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("waiterId", KeyType.HASH),
                        new("startDateTime", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new()
                {
                    IndexName = "tableKey-start-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("tableKey", KeyType.HASH),
                        new("startDateTime", KeyType.RANGE)
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
    public async Task QueryByCustomerAsync_ShouldReturnInsertedReservation()
    {
        using var client = await CreateClientAsync();
        await EnsureReservationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new ReservationRepository(ctx);

        var id = Guid.NewGuid().ToString("N");
        var start = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var end = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(90).ToString("O");

        var item = new Reservation
        {
            Id = id,
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = start,
            EndDateTime = end,
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await ctx.SaveAsync(item);

        var found = await repo.QueryByCustomerAsync("customer-1");
        found.Should().ContainSingle(x => x.Id == id);
    }

    [Fact]
    public async Task QueryByWaiterAsync_ShouldReturnInsertedReservation()
    {
        using var client = await CreateClientAsync();
        await EnsureReservationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new ReservationRepository(ctx);

        var id = Guid.NewGuid().ToString("N");
        var start = DateTimeOffset.UtcNow.AddDays(2).ToString("O");
        var end = DateTimeOffset.UtcNow.AddDays(2).AddMinutes(90).ToString("O");

        var item = new Reservation
        {
            Id = id,
            CustomerId = "customer-x",
            WaiterId = "waiter-x",
            LocationId = "loc-1",
            TableNumber = 1,
            TableKey = "loc-1#1",
            StartDateTime = start,
            EndDateTime = end,
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await ctx.SaveAsync(item);

        var found = await repo.QueryByWaiterAsync("waiter-x");
        found.Should().Contain(x => x.Id == id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnInsertedReservation()
    {
        using var client = await CreateClientAsync();
        await EnsureReservationsTableAsync(client);

        var ctx = new DynamoDBContext(client);
        var repo = new ReservationRepository(ctx);

        var id = Guid.NewGuid().ToString("N");
        var start = DateTimeOffset.UtcNow.AddDays(3).ToString("O");

        await ctx.SaveAsync(new Reservation
        {
            Id = id,
            CustomerId = "customer-y",
            WaiterId = "waiter-y",
            LocationId = "loc-1",
            TableNumber = 2,
            TableKey = "loc-1#2",
            StartDateTime = start,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        });

        var loaded = await repo.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
    }
}