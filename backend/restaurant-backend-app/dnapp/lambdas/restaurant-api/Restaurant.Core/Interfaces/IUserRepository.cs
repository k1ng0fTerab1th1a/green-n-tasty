namespace Restaurant.Core.Interfaces;

public interface IUserRepository
{
    Task<User> GetUserDataById(string userId);
}