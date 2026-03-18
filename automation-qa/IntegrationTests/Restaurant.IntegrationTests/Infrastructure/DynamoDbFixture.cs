using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace Restaurant.IntegrationTests.Infrastructure;

public class DynamoDbFixture : IAsyncLifetime
{
    public IAmazonDynamoDB Client { get; private set; }
    public DynamoDBContext Context { get; private set; }

    public async Task InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT")
                       ?? "http://localhost:8000";

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = endpoint,
            UseHttp = true
        };

        Client = new AmazonDynamoDBClient(
            new BasicAWSCredentials("test", "test"), config);

        Context = new DynamoDBContext(Client);

        await EnsureUsersTableAsync();
        await EnsureReservationsTableAsync();
        await EnsureLocationsTableAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly string[] RequiredUserIndexes =
    [
        "customer-firstName-index",
        "customer-lastName-index",
        "customer-email-index"
    ];

    private async Task EnsureUsersTableAsync()
    {
        const string tableName = "Users";

        var existing = await Client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
        {
            var table = await Client.DescribeTableAsync(tableName);
            var existingIndexes = table.Table.GlobalSecondaryIndexes?
                .Select(x => x.IndexName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            var hasAllIndexes = RequiredUserIndexes.All(existingIndexes.Contains);
            if (hasAllIndexes)
                return;

            await Client.DeleteTableAsync(tableName);

            while (true)
            {
                var tables = await Client.ListTablesAsync();
                if (!tables.TableNames.Contains(tableName))
                    break;

                await Task.Delay(500);
            }
        }

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("userId", ScalarAttributeType.S),
                new("role", ScalarAttributeType.S),
                new("firstNameNormalized", ScalarAttributeType.S),
                new("lastNameNormalized", ScalarAttributeType.S),
                new("emailNormalized", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new("userId", KeyType.HASH)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new()
                {
                    IndexName = "customer-firstName-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("role", KeyType.HASH),
                        new("firstNameNormalized", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new()
                {
                    IndexName = "customer-lastName-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("role", KeyType.HASH),
                        new("lastNameNormalized", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new()
                {
                    IndexName = "customer-email-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("role", KeyType.HASH),
                        new("emailNormalized", KeyType.RANGE)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            }
        };

        await Client.CreateTableAsync(request);

        while (true)
        {
            var desc = await Client.DescribeTableAsync(tableName);
            if (desc.Table.TableStatus == TableStatus.ACTIVE)
                break;

            await Task.Delay(500);
        }
    }

    private async Task EnsureLocationsTableAsync()
    {
        const string tableName = "Locations";

        var existing = await Client.ListTablesAsync();
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

        await Client.CreateTableAsync(request);

        while (true)
        {
            var desc = await Client.DescribeTableAsync(tableName);
            if (desc.Table.TableStatus == TableStatus.ACTIVE)
                break;

            await Task.Delay(500);
        }
    }

    private async Task EnsureReservationsTableAsync()
    {
        var tableName = "Reservations";

        var existing = await Client.ListTablesAsync();
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

        await Client.CreateTableAsync(request);

        while (true)
        {
            var desc = await Client.DescribeTableAsync(tableName);
            if (desc.Table.TableStatus == TableStatus.ACTIVE)
                break;

            await Task.Delay(500);
        }
    }
}


