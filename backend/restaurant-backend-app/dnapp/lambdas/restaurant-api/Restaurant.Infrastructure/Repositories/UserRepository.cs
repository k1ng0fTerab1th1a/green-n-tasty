using Amazon.DynamoDBv2.DataModel;
using Restaurant.Core.Interfaces;

namespace Restaurant.Infrastructure.Repositories;



public class UserRepository(IDynamoDBContext context) : IUserRepository
{
    public async Task<User> GetUserDataById(string userId)
    {
        return await context.LoadAsync<User>(userId);
    }
}