using FluentResults;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Errors;

namespace Restaurant.Api.Extensions;

public static class BusinessErrorExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this IError error)
    {
        if (error is not BusinessError businessError)
        {
            throw new InvalidDataException("Currently all the errors in Result must be BusinessErrors.");
        }

        int statusCode = businessError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status400BadRequest,
            _ => throw new InvalidDataException("ErrorType must be NotFound, Validation or Conflict.")
        };

        return ApiResponse<T>.Fail(statusCode, businessError.Message);
    }
}
