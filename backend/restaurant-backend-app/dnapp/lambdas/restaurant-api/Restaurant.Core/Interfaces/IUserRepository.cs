namespace Restaurant.Core.Interfaces;

public interface IUserRepository
{
    Task CreateAsync(User user);
}