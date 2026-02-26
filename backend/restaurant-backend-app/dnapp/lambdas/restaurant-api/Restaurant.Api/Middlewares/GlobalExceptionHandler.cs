using Microsoft.AspNetCore.Diagnostics;
using Restaurant.Api.Models;
using Restaurant.Core.Exceptions;

namespace Restaurant.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var (statusCode, message) = exception switch
        {
            UserAlreadyExistsException ex => (StatusCodes.Status409Conflict, ex.Message),
            InvalidCredentialsException ex => (StatusCodes.Status401Unauthorized, ex.Message),

            _ => (StatusCodes.Status500InternalServerError, exception.Message)
        };

        httpContext.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(statusCode, message);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}