using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using FluentAssertions;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.Infrastructure.IntegrationTests
{
    public sealed class LocationRepositoryIntegrationTests
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

        private static async Task EnsureLocationsTableAsync(IAmazonDynamoDB client)
        {
            var tableName = "Locations";

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
    }
}
