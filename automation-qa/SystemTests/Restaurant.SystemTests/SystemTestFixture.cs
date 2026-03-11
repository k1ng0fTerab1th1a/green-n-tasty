using Amazon;

namespace Restaurant.SystemTests;

public class SystemTestFixture
{
    public HttpClient Client { get; }
    public RegionEndpoint Region { get; }
    public string UserPoolId { get; }
    public string UsersTable { get; }
    public string Password { get; }

    public SystemTestFixture()
    {
        var baseUrl = Environment.GetEnvironmentVariable("SYSTEM_BASE_URL")
                      ?? throw new InvalidOperationException("SYSTEM_BASE_URL missing");

        var regionName = Environment.GetEnvironmentVariable("SYSTEM_AWS_REGION")
                 ?? throw new InvalidOperationException("SYSTEM_AWS_REGION missing");

        UserPoolId = Environment.GetEnvironmentVariable("SYSTEM_COGNITO_USER_POOL_ID")
                     ?? throw new InvalidOperationException("SYSTEM_COGNITO_USER_POOL_ID missing");

        Region = RegionEndpoint.GetBySystemName(regionName);

        Password = Environment.GetEnvironmentVariable("SYSTEM_TEST_PASSWORD") ?? "Pass12345!A";

        UsersTable = Environment.GetEnvironmentVariable("SYSTEM_USERS_TABLE") ?? "Users";

        Client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(50)
        };

        EnsureSafeEnvironment(baseUrl);
    }

    private static void EnsureSafeEnvironment(string baseUrl)
    {
        if (baseUrl.Contains("prod", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("System tests cannot run on production.");
    }
}

