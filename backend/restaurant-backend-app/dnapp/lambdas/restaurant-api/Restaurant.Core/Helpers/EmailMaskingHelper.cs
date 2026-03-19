namespace Restaurant.Core.Helpers;

public static class EmailMaskingHelper
{
    public static string Mask(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return email;

        var localPart = parts[0];
        var domainPart = parts[1];

        if (localPart.Length <= 1)
            return $"*@{domainPart}";

        if (localPart.Length == 2)
            return $"{localPart[0]}*@{domainPart}";

        if (localPart.Length <= 4)
            return $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}@{domainPart}";

        return $"{localPart[..2]}{new string('*', localPart.Length - 4)}{localPart[^2..]}@{domainPart}";
    }
}