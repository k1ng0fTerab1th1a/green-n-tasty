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
            ErrorType.NotFound     => StatusCodes.Status404NotFound,
            ErrorType.Validation   => StatusCodes.Status400BadRequest,
            ErrorType.Conflict     => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
            _ => throw new InvalidDataException("ErrorType must be NotFound, Validation, Conflict, Unauthorized or Forbidden.")
        };

        return ApiResponse<T>.Fail(statusCode, businessError.Message);
    }
}
