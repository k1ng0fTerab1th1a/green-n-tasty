namespace Restaurant.Core.Errors;

public static class AuthErrors
{
    public static BusinessError UserAlreadyExists =>
        new("User with this email already exists!", ErrorType.Conflict);

    public static BusinessError InvalidCredentials =>
        new("Invalid email or password!", ErrorType.Unauthorized);

    public static BusinessError UserNotFound =>
        new("User not found.", ErrorType.NotFound);

    public static BusinessError SignOutFailed =>
        new("Failed to sign out.", ErrorType.Validation);

    public static BusinessError RefreshTokenFailed =>
        new("Failed to refresh token.", ErrorType.Validation);

    public static BusinessError WaiterLocationNotConfigured =>
        new("Waiter location is not configured.", ErrorType.Validation);

    public static BusinessError EmailNotVerified =>
        new("Account email is not verified. Please check your inbox for a verification link.", ErrorType.Forbidden);

}
