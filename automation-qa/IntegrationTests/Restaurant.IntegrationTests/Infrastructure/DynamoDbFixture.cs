using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace Restaurant.IntegrationTests.Infrastructure;

public class DynamoDbFixture : IAsyncLifetime
{
    private static readonly string[] RequiredUserIndexes =
    [
        "customer-firstName-index",
        "customer-lastName-index",
        "customer-email-index"
    ];

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
        await EnsureOrdersTableAsync();
        await EnsureDishesTableAsync();
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
        {
            var table = await Client.DescribeTableAsync(tableName);
            var existingIndexes = table.Table.GlobalSecondaryIndexes?
                .Select(x => x.IndexName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            var hasAllIndexes = RequiredUserIndexes.All(existingIndexes.Contains);
            if (hasAllIndexes)
            {
                await WaitForTableActiveAsync(tableName);
                return;
            }

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
            AttributeDefinitions =
            [
                new AttributeDefinition("userId", ScalarAttributeType.S),
                new AttributeDefinition("role", ScalarAttributeType.S),
                new AttributeDefinition("firstNameNormalized", ScalarAttributeType.S),
                new AttributeDefinition("lastNameNormalized", ScalarAttributeType.S),
                new AttributeDefinition("emailNormalized", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("userId", KeyType.HASH)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = "customer-firstName-index",
                    KeySchema =
                    [
                        new KeySchemaElement("role", KeyType.HASH),
                        new KeySchemaElement("firstNameNormalized", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new GlobalSecondaryIndex
                {
                    IndexName = "customer-lastName-index",
                    KeySchema =
                    [
                        new KeySchemaElement("role", KeyType.HASH),
                        new KeySchemaElement("lastNameNormalized", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                },
                new GlobalSecondaryIndex
                {
                    IndexName = "customer-email-index",
                    KeySchema =
                    [
                        new KeySchemaElement("role", KeyType.HASH),
                        new KeySchemaElement("emailNormalized", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            ]
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

    private async Task EnsureOrdersTableAsync()
    {
        const string tableName = "Orders";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S),
                new AttributeDefinition("reservationId", ScalarAttributeType.S)
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
                    IndexName = "reservationId-index",
                    KeySchema =
                    [
                        new KeySchemaElement("reservationId", KeyType.HASH)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            ]
        };

        await EnsureTableAsync(request);
    }

    private async Task EnsureDishesTableAsync()
    {
        const string tableName = "Dishes";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("id", KeyType.HASH)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        };

        await EnsureTableAsync(request);
    }

    private async Task EnsureTablesTableAsync()
    {
        const string tableName = "Tables";

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("locationId", ScalarAttributeType.S),
                new AttributeDefinition("tableNumber", ScalarAttributeType.N)
            ],
            KeySchema =
            [
                new KeySchemaElement("locationId", KeyType.HASH),
                new KeySchemaElement("tableNumber", KeyType.RANGE)
            ],
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        };

        await EnsureTableAsync(request);
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
}