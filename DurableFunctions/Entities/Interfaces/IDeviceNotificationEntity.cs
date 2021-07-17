using DurableFunctions.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableFunctions.Entities.Interfaces
{
    public interface IDeviceNotificationEntity
    {
        void NotificationFired(DeviceNotificationInfo notification);
        Task PurgeAsync();
    }
}