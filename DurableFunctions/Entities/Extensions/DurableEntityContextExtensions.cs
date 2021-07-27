using DurableFunctions.Entities;
using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask
{
    public static class DurableEntityContextExtensions
    {
        public static void SignalNotification(this IDurableEntityContext context, 
            DeviceNotificationInfo notification)
        {
            var notificationEntityId = new EntityId(nameof(DeviceNotificationsEntity),
                    $"{context.EntityName}|{context.EntityKey}");

            Entity.Current.SignalEntity<IDeviceNotificationEntity>(notificationEntityId,
                n => n.NotificationFired(notification));
        }
    }
}
