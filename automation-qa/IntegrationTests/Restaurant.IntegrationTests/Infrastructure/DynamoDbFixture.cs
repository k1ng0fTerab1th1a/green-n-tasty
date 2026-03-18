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
        await EnsureTablesTableAsync();
        await EnsureTableDaysTableAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task EnsureUsersTableAsync()
    {
        const string tableName = "Users";

        var existing = await Client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
            return;

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("userId", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new("userId", KeyType.HASH)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
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

    private async Task EnsureTablesTableAsync()
    {
        const string tableName = "Tables";

        var existing = await Client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
            return;

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("locationId", ScalarAttributeType.S),
                new("tableNumber", ScalarAttributeType.N)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new("locationId", KeyType.HASH),
                new("tableNumber", KeyType.RANGE)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
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

    private async Task EnsureTableDaysTableAsync()
    {
        const string tableName = "TableDays";

        var existing = await Client.ListTablesAsync();
        if (existing.TableNames.Contains(tableName))
            return;

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("tableKey", ScalarAttributeType.S),
                new("date", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new("tableKey", KeyType.HASH),
                new("date", KeyType.RANGE)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
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


