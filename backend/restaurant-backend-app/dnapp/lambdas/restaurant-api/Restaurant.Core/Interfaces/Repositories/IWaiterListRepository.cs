using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories;

public interface IWaiterListRepository
{
    Task<bool> ContainsAsync(string email);
}
