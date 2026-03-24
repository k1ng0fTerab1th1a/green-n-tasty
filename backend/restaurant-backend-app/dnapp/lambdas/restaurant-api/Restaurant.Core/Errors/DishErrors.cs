namespace Restaurant.Core.Errors;

public static class DishErrors
{
    public static BusinessError NotFound => new("Dish not found.", ErrorType.NotFound);
}
