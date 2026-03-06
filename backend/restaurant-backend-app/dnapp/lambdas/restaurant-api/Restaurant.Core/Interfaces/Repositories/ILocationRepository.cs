using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Repositories
{
    public interface ILocationRepository
    {
        Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default);
        Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}
