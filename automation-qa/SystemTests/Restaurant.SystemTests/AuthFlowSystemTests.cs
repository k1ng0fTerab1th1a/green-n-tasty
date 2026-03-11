using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Restaurant.SystemTests;

public sealed class AuthFlowSystemTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public AuthFlowSystemTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [SystemTestFact]
    [Trait("Category", "System")]
    public async Task AuthFlow_ShouldSignUp_SignIn_Refresh_SignOut_AndPersistUser()
    {
        await using var user = new TestUser(_fixture);

        var client = _fixture.Client;

        var signUp = await client.PostAsJsonAsync("auth/sign-up", new
        {
            email = user.Email,
            password = _fixture.Password,
            firstName = "System",
            lastName = "Test"
        });

        signUp.StatusCode.Should().Be(HttpStatusCode.Created);

        var signIn = await client.PostAsJsonAsync("auth/sign-in", new
        {
            email = user.Email,
            password = _fixture.Password
        });

        signIn.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await signIn.Content.ReadAsStringAsync());
        var data = GetPropertyIgnoreCase(doc.RootElement, "data");

        var idToken = GetPropertyIgnoreCase(data, "idToken").GetString();
        var refreshToken = GetPropertyIgnoreCase(data, "refreshToken").GetString();

        idToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();

        user.UserSub = ExtractSub(idToken!);

        await AssertUserExistsInCognito(user.Email);
        await AssertUserExistsInDynamo(user.UserSub);

        var refresh = await client.PostAsJsonAsync("auth/refresh-token", new
        {
            refreshToken
        });

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var signOut = await client.PostAsJsonAsync("auth/sign-out", new
        {
            refreshToken
        });

        signOut.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SystemTestFact]
    public async Task SignIn_ShouldReturn401_WhenPasswordWrong()
    {
        await using var user = new TestUser(_fixture);

        var client = _fixture.Client;

        var signUp = await client.PostAsJsonAsync("auth/sign-up", new
        {
            email = user.Email,
            password = _fixture.Password,
            firstName = "System",
            lastName = "Test"
        });
        signUp.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("auth/sign-in", new
        {
            email = user.Email,
            password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SystemTestFact]
    public async Task SignUp_ShouldReturn409_WhenEmailExists()
    {
        await using var user = new TestUser(_fixture);

        var client = _fixture.Client;

        var first = await client.PostAsJsonAsync("auth/sign-up", new
        {
            email = user.Email,
            password = _fixture.Password,
            firstName = "System",
            lastName = "Test"
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("auth/sign-up", new
        {
            email = user.Email,
            password = _fixture.Password,
            firstName = "System",
            lastName = "Test"
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task AssertUserExistsInCognito(string email)
    {
        using var cognito = new AmazonCognitoIdentityProviderClient(_fixture.Region);

        var result = await cognito.AdminGetUserAsync(new AdminGetUserRequest
        {
            UserPoolId = _fixture.UserPoolId,
            Username = email
        });

        var emailAttr = result.UserAttributes
            .FirstOrDefault(x => x.Name == "email")?.Value;

        emailAttr.Should().Be(email);
    }
    private async Task AssertUserExistsInDynamo(string userSub)
    {
        using var dynamo = new AmazonDynamoDBClient(_fixture.Region);

        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.UsersTable,
            Key = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = userSub }
            }
        });

        response.Item.Should().NotBeNullOrEmpty();
    }

    private static string ExtractSub(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);
        return jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
    }
    private static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return p.Value;
        }

        throw new KeyNotFoundException($"Property '{propertyName}' was not found.");
    }
}
