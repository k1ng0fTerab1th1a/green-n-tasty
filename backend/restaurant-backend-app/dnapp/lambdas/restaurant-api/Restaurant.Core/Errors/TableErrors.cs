namespace Restaurant.Core.Errors;

public static class TableErrors
{
    public static BusinessError RequestedSlotsFromPast =>
        new("Cannot find available slots in the past.", ErrorType.Validation);

    public static BusinessError RequestedSlotsFromFarFuture =>
        new("The date requested is too far in the future.", ErrorType.Validation);
}
