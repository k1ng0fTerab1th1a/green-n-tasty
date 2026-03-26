using Restaurant.Core.Errors;

namespace Restaurant.Reports.Domain.Errors;

public static class ReportErrors
{
    public static BusinessError InvalidDateRange =>
        new("'From' date must be earlier than or equal to 'To' date.", ErrorType.Validation);
}
