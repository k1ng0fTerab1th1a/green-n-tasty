using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDynamoDBContext _context;

    public UserRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(User user)
    {
        await _context.SaveAsync(user);
    }
}