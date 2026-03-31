namespace Restaurant.Core.Errors;

public static class UserErrors
{
    public static BusinessError UserNotFound =>
        new("User not found.", ErrorType.NotFound);

    public static BusinessError EmailAlreadyInUse =>
        new("This email is already in use.", ErrorType.Conflict);

    public static BusinessError UpdateNotSuccessful =>
        new("User update was not successful", ErrorType.Validation);
}
