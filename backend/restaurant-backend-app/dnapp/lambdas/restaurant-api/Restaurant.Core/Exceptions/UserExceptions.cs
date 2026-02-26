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