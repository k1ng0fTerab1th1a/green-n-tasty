namespace Restaurant.UnitTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ManualFactAttribute : FactAttribute
{
    public ManualFactAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("RUN_MANUAL_TESTS");
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Manual-only test. Set RUN_MANUAL_TESTS=true to run.";
        }
    }
}
