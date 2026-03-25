using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task CreateAsync(User user);
    Task<User?> GetByIdAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct);
}