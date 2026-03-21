namespace Restaurant.Core.Errors;

public static class LocationErrors
{
    public static BusinessError NotFound => new("Location not found.", ErrorType.NotFound);
}
