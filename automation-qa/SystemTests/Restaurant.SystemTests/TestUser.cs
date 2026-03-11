using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Restaurant.SystemTests;
public sealed class TestUser : IAsyncDisposable
{
    private readonly SystemTestFixture _fixture;

    public string Email { get; }
    public string? UserSub { get; set; }

    public TestUser(SystemTestFixture fixture)
    {
        _fixture = fixture;
        Email = $"systemtest+{Guid.NewGuid():N}@example.com";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            using var cognito = new AmazonCognitoIdentityProviderClient(new AmazonCognitoIdentityProviderConfig
            {
                RegionEndpoint = _fixture.Region,
                Timeout = TimeSpan.FromSeconds(15),
                ReadWriteTimeout = TimeSpan.FromSeconds(15)
            });

            await cognito.AdminDeleteUserAsync(new AdminDeleteUserRequest
            {
                UserPoolId = _fixture.UserPoolId,
                Username = Email
            });
        }
        catch { }

        if (string.IsNullOrWhiteSpace(UserSub))
            return;

        try
        {
            using var dynamo = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                RegionEndpoint = _fixture.Region,
                Timeout = TimeSpan.FromSeconds(15),
                ReadWriteTimeout = TimeSpan.FromSeconds(15)
            });

            await dynamo.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _fixture.UsersTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = UserSub }
                }
            });
        }
        catch { }
    }
}