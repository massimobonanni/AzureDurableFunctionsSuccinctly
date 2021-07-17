using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Entities.Models;
using DurableFunctions.Entities.TemperatureDevice;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.Entities
{
    public class EntityFactory : IEntityFactory
    {
        public Task<EntityId> GetEntityIdAsync(string deviceId, DeviceType type, CancellationToken token)
        {
            EntityId entityId;
            switch (type)
            {
                case DeviceType.Temperature:
                    entityId = new EntityId(nameof(TemperatureDeviceEntity), deviceId);
                    break;
                default:
                    break;
            }
            return Task.FromResult(entityId);
        }

        public Task<string> GetEntityNameAsync(DeviceType type, CancellationToken token)
        {
            string entityName = null;
            switch (type)
            {
                case DeviceType.Temperature:
                    entityName = nameof(TemperatureDeviceEntity);
                    break;
                default:
                    break;
            }
            return Task.FromResult(entityName);
        }
    }
}
