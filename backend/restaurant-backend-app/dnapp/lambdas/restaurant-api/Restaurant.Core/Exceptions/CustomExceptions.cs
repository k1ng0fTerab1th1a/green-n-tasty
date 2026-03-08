namespace Restaurant.Core.Exceptions;
public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException() : base("User with this email already exists!") { }
    public UserAlreadyExistsException(string message) : base(message) { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password!") { }

    public InvalidCredentialsException(string message) : base(message) { }
}

public class AuthException : Exception
{
    public AuthException() : base("Authorization failed!") { }
    public AuthException(string message) : base(message) { }
}

public sealed class SlotUnavailableException : Exception
{
    public SlotUnavailableException() : base("The selected time is already taken.") { }

    public SlotUnavailableException(string message) : base(message) { }
}

public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}