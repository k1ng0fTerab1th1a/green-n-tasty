using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces;

public interface IWaiterListRepository
{
    Task<bool> ContainsAsync(string email);
}
