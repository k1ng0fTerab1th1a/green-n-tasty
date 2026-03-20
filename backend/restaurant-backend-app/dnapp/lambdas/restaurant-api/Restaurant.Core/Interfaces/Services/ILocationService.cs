using FluentResults;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services;

public interface ILocationService
{
    Task<Result<IReadOnlyList<Location>>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Location>>> GetLocationOptionsAsync(CancellationToken cancellationToken = default);
}
