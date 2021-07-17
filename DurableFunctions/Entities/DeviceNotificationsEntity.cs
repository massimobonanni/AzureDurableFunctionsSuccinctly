using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Entities.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Entities
{
    public class DeviceNotificationsEntity : IDeviceNotificationEntity
    {
        private readonly ILogger logger;

        public DeviceNotificationsEntity(ILogger logger)
        {
            this.logger = logger;
        }

        #region [ State ]

        [JsonProperty("notifications")]
        public List<DeviceNotificationInfo> Notifications { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        #endregion [ State ]

        #region [ Behaviour ]

        public void NotificationFired(DeviceNotificationInfo notification)
        {
            if (notification == null)
                return;

            if (Notifications == null)
                Notifications = new List<DeviceNotificationInfo>();

            DeviceType = notification.DeviceType;
            DeviceId = notification.DeviceId;
            Notifications.Add(notification);
        }

        public Task PurgeAsync()
        {
            Notifications?.Clear();
            return Task.CompletedTask;
        }

        #endregion [ Behaviour ]

        [FunctionName(nameof(DeviceNotificationsEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger logger)
               => ctx.DispatchAsync<DeviceNotificationsEntity>(logger);

    }
}
