namespace Restaurant.Core.Errors;

public static class OrderErrors
{
    public static readonly BusinessError ReservationNotFound =
        new("Reservation not found.", ErrorType.NotFound);

    public static readonly BusinessError OrderNotFound =
        new("Order not found.", ErrorType.NotFound);

    public static readonly BusinessError ReservationStatusNotOrderable =
        new("Reservation status not in progress.", ErrorType.Validation);

    public static readonly BusinessError ReservationStatusNotCompletable =
        new("Reservation status does not allow order completion.", ErrorType.Validation);

    public static readonly BusinessError Forbidden =
        new("You are not assigned to this reservation.", ErrorType.Forbidden);

    public static readonly BusinessError NoDishesProvided =
        new("Add dishes to the order.", ErrorType.Validation);

    public static readonly BusinessError OrderAlreadyExists =
        new("Order already exists for this reservation.", ErrorType.Conflict);

    public static readonly BusinessError ReservationStateConflict =
        new("Reservation state changed. Please refresh and retry.", ErrorType.Conflict);

    public static readonly BusinessError OrderStateConflict =
        new("Order state changed. Please refresh and retry.", ErrorType.Conflict);

    public static readonly BusinessError OrderNotOpen =
        new("Only open order can be changed.", ErrorType.Validation);

    public static BusinessError DishNotAvailable(string dishId) =>
        new($"Dish '{dishId}' is not available.", ErrorType.Validation);

    public static BusinessError DishNotFound(string dishId) =>
        new($"Dish '{dishId}' not found.", ErrorType.NotFound);

    public static BusinessError DishNotFoundInOrder(string dishId) =>
        new($"Dish '{dishId}' not found in order.", ErrorType.NotFound);

    public static BusinessError InvalidDishRemovalQuantity(string dishId) =>
        new($"Cannot remove more items than currently exist for dish '{dishId}'.", ErrorType.Validation);
}
