namespace Restaurant.Core.Errors;

public static class ReceiptErrors
{
    public static BusinessError ReservationNotFinished => new("Receipt is only available for finished reservations.", ErrorType.Validation);
    public static BusinessError OrderNotFound => new("No order found for this reservation.", ErrorType.NotFound);
    public static BusinessError VisitorSecretCodeMissing => new("Secret code for visitor feedback is unavailable.", ErrorType.Validation);
    public static BusinessError Forbidden => new("Only the assigned waiter can retrieve this receipt.", ErrorType.Forbidden);
}
