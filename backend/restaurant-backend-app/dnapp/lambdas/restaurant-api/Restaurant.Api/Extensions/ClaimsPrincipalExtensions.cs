using System.Security.Claims;

namespace Restaurant.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            if (user is null) return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;
        }

        public static bool IsWaiter(this ClaimsPrincipal user)
        {
            if (user is null) return false;

            var role = user.FindFirst("custom:role")?.Value
                       ?? user.FindFirst(ClaimTypes.Role)?.Value;

            return string.Equals(role, "WAITER", StringComparison.OrdinalIgnoreCase);
        }
    }
}
