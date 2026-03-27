using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task CreateAsync(User user, CancellationToken ct);
    Task<User?> GetByIdAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct);
    Task UpdateEmailAsync(string userId, string newEmail, CancellationToken ct);
}