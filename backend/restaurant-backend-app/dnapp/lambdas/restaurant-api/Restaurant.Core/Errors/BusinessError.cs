using FluentResults;

namespace Restaurant.Core.Errors;

public enum ErrorType { NotFound, Validation, Conflict }

public sealed class BusinessError : Error
{
    public ErrorType Type { get; }

    public BusinessError(string message, ErrorType type) : base(message)
    {
        Type = type;
    }
}
