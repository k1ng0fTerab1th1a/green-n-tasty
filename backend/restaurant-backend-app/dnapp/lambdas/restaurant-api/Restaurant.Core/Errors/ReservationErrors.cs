namespace Restaurant.Core.Errors;

public static class ReservationErrors
{
    public static BusinessError ReservationNotFound => new("Reservation not found.", ErrorType.NotFound);
    public static BusinessError Forbidden => new("Forbidden.", ErrorType.Forbidden);

    public static BusinessError NotCancellable => new("Only reserved reservations can be cancelled.", ErrorType.Validation);
    public static BusinessError TooLateToCancel => new("Reservation cannot be cancelled less than 30 minutes before it starts.", ErrorType.Validation);
    public static BusinessError CancellationFailed => new("Failed to cancel reservation.", ErrorType.Validation);

    public static BusinessError NotUpdatable => new("Only reserved reservations can be updated.", ErrorType.Validation);
    public static BusinessError TooLateToUpdate => new("Reservation cannot be updated less than 30 minutes before it starts.", ErrorType.Validation);
    public static BusinessError UpdateFailed => new("Failed to update reservation.", ErrorType.Validation);

    public static BusinessError LocationNotFound => new("Location not found.", ErrorType.NotFound);
    public static BusinessError NoWaiterAssigned => new("No waiter assigned for this table on this date.", ErrorType.Validation);
    public static BusinessError SlotUnavailable => new("The selected time is already taken.", ErrorType.Conflict);

    public static BusinessError CustomerOrVisitorRequired => new("Exactly one of customerId or visitorName must be provided.", ErrorType.Validation);
    public static BusinessError CustomerNotFound => new("Customer not found.", ErrorType.NotFound);
    public static BusinessError WaiterNotAssignedForCreation => new("Waiter can create reservations only for assigned tables.", ErrorType.Validation);
    public static BusinessError WaiterNotAssignedForUpdate => new("Waiter can update reservations only for assigned tables.", ErrorType.Validation);

    public static BusinessError TableNotFound => new("Table not found.", ErrorType.NotFound);
    public static BusinessError TableCapacityExceeded => new("Amount of guests exceeds the table capacity.", ErrorType.Validation);

    public static BusinessError OutsideWorkingHours(string openTime, string closeTime) =>
        new($"Reservation must be within working hours ({openTime} - {closeTime}).", ErrorType.Validation);
    public static BusinessError StartTimeNotAligned => new("Start time must be a multiple of 15 minutes.", ErrorType.Validation);
    public static BusinessError EndTimeNotAligned => new("End time must be a multiple of 15 minutes.", ErrorType.Validation);
    public static BusinessError DurationTooShort => new("Minimum booking duration is 60 minutes.", ErrorType.Validation);
    public static BusinessError DurationTooLong => new("Maximum booking duration is 6 hours.", ErrorType.Validation);
    public static BusinessError PastDateTime => new("Cannot book for a past date or time.", ErrorType.Validation);
}
