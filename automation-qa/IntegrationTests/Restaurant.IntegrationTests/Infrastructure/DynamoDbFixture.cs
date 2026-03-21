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
        await EnsureDishesTableAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task EnsureUsersTableAsync()
    {
        const string tableName = "Users";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("userId", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("userId", KeyType.HASH)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        };

        await EnsureTableAsync(request);
    }

    private async Task EnsureLocationsTableAsync()
    {
        const string tableName = "Locations";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S),
                new AttributeDefinition("entityType", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("id", KeyType.HASH)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = "entityType-index",
                    KeySchema =
                    [
                        new KeySchemaElement("entityType", KeyType.HASH),
                        new KeySchemaElement("id", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            ]
        };

        await EnsureTableAsync(request);
    }

    private async Task EnsureReservationsTableAsync()
    {
        const string tableName = "Reservations";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S),
                new AttributeDefinition("customerId", ScalarAttributeType.S),
                new AttributeDefinition("waiterId", ScalarAttributeType.S),
                new AttributeDefinition("tableKey", ScalarAttributeType.S),
                new AttributeDefinition("startDateTime", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("id", KeyType.HASH)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = "customerId-start-index",
                    KeySchema =
                    [
                        new KeySchemaElement("customerId", KeyType.HASH),
                        new KeySchemaElement("startDateTime", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new GlobalSecondaryIndex
                {
                    IndexName = "waiterId-start-index",
                    KeySchema =
                    [
                        new KeySchemaElement("waiterId", KeyType.HASH),
                        new KeySchemaElement("startDateTime", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new GlobalSecondaryIndex
                {
                    IndexName = "tableKey-start-index",
                    KeySchema =
                    [
                        new KeySchemaElement("tableKey", KeyType.HASH),
                        new KeySchemaElement("startDateTime", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            ]
        };

        await EnsureTableAsync(request);
    }

    private async Task EnsureTableAsync(CreateTableRequest request)
    {
        try
        {
            await Client.CreateTableAsync(request);
        }
        catch (ResourceInUseException)
        {
            // Another test already created this table.
        }

        await WaitForTableActiveAsync(request.TableName);
    }

    private async Task WaitForTableActiveAsync(string tableName)
    {
        while (true)
        {
            try
            {
                var desc = await Client.DescribeTableAsync(tableName);
                if (desc.Table.TableStatus == TableStatus.ACTIVE)
                    break;
            }
            catch (ResourceNotFoundException)
            {
                // Creation is eventually consistent; retry until visible.
            }

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

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("tableKey", ScalarAttributeType.S),
                new AttributeDefinition("date", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("tableKey", KeyType.HASH),
                new KeySchemaElement("date", KeyType.RANGE)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        };

        await EnsureTableAsync(request);
    }
    
    
    private async Task EnsureDishesTableAsync()
    {
        const string tableName = "Dishes";
 
        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("id",                   ScalarAttributeType.S),
                new("popularityFlag",       ScalarAttributeType.S),
                new("specialityForLocation", ScalarAttributeType.S)
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
                    IndexName = "PopularDishesIndex",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("popularityFlag", KeyType.HASH)
                    },
                    Projection            = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new()
                {
                    IndexName = "SpecialityIndex",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("specialityForLocation", KeyType.HASH)
                    },
                    Projection            = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            }
        };
 
        await EnsureTableAsync(request);
    }

}
