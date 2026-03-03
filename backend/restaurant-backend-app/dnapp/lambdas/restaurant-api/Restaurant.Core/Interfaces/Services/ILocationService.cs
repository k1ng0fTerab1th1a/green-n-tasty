using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restaurant.Core.Models;

namespace Restaurant.Core.Interfaces.Services
{
    public interface ILocationService
    {
        Task<IReadOnlyList<Location>> GetLocationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Location>> GetLocationOptionsAsync(CancellationToken cancellationToken = default);
    }
}
