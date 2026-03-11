using Xunit;

namespace Restaurant.SystemTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SystemTestFactAttribute : FactAttribute
{
    public SystemTestFactAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("RUN_SYSTEM_TESTS");
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "System test is skipped. Set RUN_SYSTEM_TESTS=true to run.";
        }
    }
}
