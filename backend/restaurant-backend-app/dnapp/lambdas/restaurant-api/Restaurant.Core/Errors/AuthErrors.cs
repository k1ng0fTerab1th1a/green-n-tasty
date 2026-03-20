namespace Restaurant.Core.Errors;

public static class AuthErrors
{
    public static BusinessError UserAlreadyExists =>
        new("User with this email already exists!", ErrorType.Conflict);

    public static BusinessError InvalidCredentials =>
        new("Invalid email or password!", ErrorType.Unauthorized);
}
