using FluentResults;

namespace Restaurant.Core.Errors;

public enum ErrorType { NotFound, Validation, Conflict, Unauthorized, Forbidden }

public sealed class BusinessError : Error
{
    public ErrorType Type { get; }

    public BusinessError(string message, ErrorType type) : base(message)
    {
        Type = type;
    }

    public override bool Equals(object? obj) =>
        obj is BusinessError other && Message == other.Message && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Message, Type);
}
