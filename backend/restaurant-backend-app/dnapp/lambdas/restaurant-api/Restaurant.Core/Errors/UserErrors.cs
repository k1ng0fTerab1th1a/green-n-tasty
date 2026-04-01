namespace Restaurant.Core.Errors;

public static class UserErrors
{
    public static BusinessError UserNotFound =>
        new("User not found.", ErrorType.NotFound);
    
    public static BusinessError OtpNotFound =>
        new("User OTP does not exist.", ErrorType.NotFound);
    
    public static BusinessError OtpAlreadyUsed =>
        new("This OTP was already used.", ErrorType.Validation);
    
    public static BusinessError OtpExpired =>
        new("This OTP is expired.", ErrorType.Validation);
    
    public static BusinessError OtpIsWrong =>
        new("This OTP is not correct.", ErrorType.Validation);

    public static BusinessError EmailAlreadyInUse =>
        new("This email is already in use.", ErrorType.Conflict);

    public static BusinessError EmptyEmail =>
        new("Email cannot be empty", ErrorType.Validation);

    public static BusinessError UpdateNotSuccessful =>
        new("User update was not successful", ErrorType.Validation);
    
    public static BusinessError PasswordRecoveryUnsuccessful =>
        new("Password Recovery request was unsuccessful", ErrorType.Validation);
    
}
