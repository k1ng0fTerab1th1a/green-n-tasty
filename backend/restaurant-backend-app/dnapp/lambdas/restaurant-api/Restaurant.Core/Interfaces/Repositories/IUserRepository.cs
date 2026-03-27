using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task CreateAsync(User user);
    Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct = default);

    Task<(string username, string? iamgeUrl)> GetUserDataForFeedbackCreationByIdAsync(string userId,
        CancellationToken ct = default);

    Task<(double rating, int feedbacksAmount)> GetUserFeedbackRatingDataByIdAsync(string userId,
        CancellationToken ct = default);

    Task UpdateUserRatingAsync(string userId, int newFeedbackRating, CancellationToken ct = default);
    Task<WaiterFeedbackData> GetWaiterFeedbackDataAsync(string waiterId, CancellationToken ct = default);
}