using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDynamoDBContext _context;

    private const string CustomerRole = "CUSTOMER";

    public UserRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(User user)
    {
        await _context.SaveAsync(user);
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        return _context.LoadAsync<User?>(userId, ct);
    }

    public async Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = query.Trim();

        var search = _context.ScanAsync<User>(
        [
            new ScanCondition(nameof(User.Role), ScanOperator.Equal, CustomerRole)
        ]);

        var customers = await search.GetRemainingAsync(ct);

        return customers
            .Where(x => MatchesQuery(x, normalizedQuery))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();
    }

    private static bool MatchesQuery(User user, string query)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return fullName.Contains(query, StringComparison.OrdinalIgnoreCase)
               || user.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}