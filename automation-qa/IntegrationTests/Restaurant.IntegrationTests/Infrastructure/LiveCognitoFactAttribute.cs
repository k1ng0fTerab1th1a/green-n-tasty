namespace Restaurant.Infrastructure.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LiveCognitoFactAttribute : FactAttribute
{
    public LiveCognitoFactAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("RUN_LIVE_COGNITO_TESTS");
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_LIVE_COGNITO_TESTS=true to enable live Cognito tests.";
        }
    }
}
