namespace Restaurant.Core.Interfaces.Repositories;

public interface ITableRepository
{
    Task<IReadOnlyList<Table>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Table>> GetByLocationIdAsync(string locationId, CancellationToken ct);
}
