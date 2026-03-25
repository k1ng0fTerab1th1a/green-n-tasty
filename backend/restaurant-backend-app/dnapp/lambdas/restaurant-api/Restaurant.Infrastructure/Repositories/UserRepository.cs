using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;

namespace Restaurant.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private const string CustomerRole = "CUSTOMER";
    private const string FirstNameIndex = "customer-firstName-index";
    private const string LastNameIndex = "customer-lastName-index";
    private const string EmailIndex = "customer-email-index";

    private readonly IDynamoDBContext _context;

    public UserRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        ApplySearchFields(user);
        await _context.SaveAsync(user, ct);
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        return _context.LoadAsync<User?>(userId, ct);
    }

    public async Task<IReadOnlyList<User>> SearchCustomersAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<User>();

        var normalizedQuery = Normalize(query);

        var byFirstNameTask = QueryByPrefixAsync(FirstNameIndex, normalizedQuery, ct);
        var byLastNameTask = QueryByPrefixAsync(LastNameIndex, normalizedQuery, ct);
        var byEmailTask = QueryByPrefixAsync(EmailIndex, normalizedQuery, ct);

        await Task.WhenAll(byFirstNameTask, byLastNameTask, byEmailTask);

        return byFirstNameTask.Result
            .Concat(byLastNameTask.Result)
            .Concat(byEmailTask.Result)
            .GroupBy(x => x.UserId)
            .Select(x => x.First())
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();
    }

    private Task<List<User>> QueryByPrefixAsync(string indexName, string normalizedPrefix, CancellationToken ct)
    {
        var config = new DynamoDBOperationConfig
        {
            IndexName = indexName
        };

        var search = _context.QueryAsync<User>(
            CustomerRole,
            QueryOperator.BeginsWith,
            new object[] { normalizedPrefix },
            config);

        return search.GetRemainingAsync(ct);
    }

    private static void ApplySearchFields(User user)
    {
        user.Role = user.Role.Trim().ToUpperInvariant();
        user.FirstNameNormalized = Normalize(user.FirstName);
        user.LastNameNormalized = Normalize(user.LastName);
        user.EmailNormalized = Normalize(user.Email);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}