using Restaurant.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Interfaces.Repositories
{
    public interface IWaiterScheduleRepository
    {
        Task<WaiterSchedule?> GetAsync(string tableKey, string date, CancellationToken ct = default);
    }
}
