using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Restaurant.Api.Contracts.Responses;

public class ApiResponse<T> : IActionResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    [JsonIgnore]
    public int StatusCode { get; set; }
    public static ApiResponse<T> Success(int statusCode, T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(int statusCode, string message)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Data = default,
            Message = message
        };
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var body = new
        {
            IsSuccess,
            Message,
            Data
        };

        var objectResult = new ObjectResult(body)
        {
            StatusCode = this.StatusCode
        };

        await objectResult.ExecuteResultAsync(context);
    }
}