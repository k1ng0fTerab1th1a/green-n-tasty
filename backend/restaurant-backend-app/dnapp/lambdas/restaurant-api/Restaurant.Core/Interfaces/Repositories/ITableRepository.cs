namespace Restaurant.Core.Interfaces.Repositories;

public interface ITableRepository
{
    Task<IReadOnlyList<Table>> GetAllAsync();
    Task<IReadOnlyList<Table>> GetByLocationIdAsync(string locationId);
}
