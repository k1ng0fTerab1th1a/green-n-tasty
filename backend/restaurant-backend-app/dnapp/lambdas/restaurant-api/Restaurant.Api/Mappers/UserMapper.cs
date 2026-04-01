using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Models;

namespace Restaurant.Api.Mappers;

public static class UserMapper
{
    public static UserResponse ToResponse(this User x) => new()
    {
        UserId = x.UserId,
        FirstName = x.FirstName,
        LastName = x.LastName,
        Email = x.Email,
        Role = x.Role,
        WaiterFlag = x.WaiterFlag,
        ImageUrl = x.ImageUrl,
        Rating = x.FeedbacksCount <= 0 ? 0 : (double)x.TotalRating / x.FeedbacksCount,
        FeedbacksCount = x.FeedbacksCount
    };
}
