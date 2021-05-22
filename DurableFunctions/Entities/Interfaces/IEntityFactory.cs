using DurableFunctions.Entities.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.Entities.Interfaces
{
    public interface IEntityFactory
    {
        Task<EntityId> GetEntityIdAsync(string deviceId, DeviceType type, CancellationToken token);
    }
}
