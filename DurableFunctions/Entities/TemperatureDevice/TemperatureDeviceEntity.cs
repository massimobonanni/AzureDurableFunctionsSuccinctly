using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Entities.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DurableFunctions.Entities.TemperatureDevice
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TemperatureDeviceEntity : IDeviceEntity
    {
        public class DeviceConfiguration
        {
            [JsonProperty("historyRetention")]
            public TimeSpan HistoryRetention { get; set; } = TimeSpan.FromMinutes(10);

            [JsonProperty("temperatureHighThreshold")]
            public double? TemperatureHighThreshold { get; set; }

            [JsonProperty("temperatureLowThreshold")]
            public double? TemperatureLowThreshold { get; set; }

            [JsonProperty("notificationNumber")]
            public string NotificationNumber { get; set; }

            public bool TemperatureHighAlertEnabled()
            {
                return TemperatureHighThreshold.HasValue;
            }

            public bool TemperatureLowAlertEnabled()
            {
                return TemperatureLowThreshold.HasValue;
            }
        }

        private readonly ILogger logger;

        public TemperatureDeviceEntity(ILogger logger)
        {
            this.logger = logger;
            EntityConfig = new DeviceConfiguration();
        }

        #region [ State ]

        [JsonProperty("deviceType")]
        public string DeviceType { get => Models.DeviceType.Temperature.ToString(); set { } }

        [JsonProperty("historyData")]
        public Dictionary<DateTimeOffset, DeviceData> HistoryData { get; set; }

        [JsonProperty("entityConfig")]
        public DeviceConfiguration EntityConfig { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("lastUpdate")]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonProperty("lastData")]
        public DeviceData LastData { get; set; }

        [JsonProperty("temperatureHighNotificationFired")]
        public bool TemperatureHighNotificationFired { get; set; } = false;

        [JsonProperty("temperatureLowNotificationFired")]
        public bool TemperatureLowNotificationFired { get; set; } = false;
        #endregion [ State ]

        #region [ Behaviour ]
        public void TelemetryReceived(DeviceTelemetry telemetry)
        {
            if (HistoryData == null)
                HistoryData = new Dictionary<DateTimeOffset, DeviceData>();

            DeviceName = telemetry.DeviceName;

            if (telemetry.Timestamp < DateTimeOffset.Now.Subtract(EntityConfig.HistoryRetention))
                return;

            if (telemetry.Data != null)
            {
                HistoryData[telemetry.Timestamp] = telemetry.Data;

                if (LastUpdate < telemetry.Timestamp)
                {
                    LastUpdate = telemetry.Timestamp;
                    LastData = telemetry.Data;
                }

                ClearHistoryData();
                CheckAlert();
            }
        }
        public Task<IDictionary<DateTimeOffset, DeviceData>> GetLastTelemetries(int numberOfTelemetries = 10)
        {
            IDictionary<DateTimeOffset, DeviceData> telemetryList = null;
            if (HistoryData != null)
            {
                telemetryList = HistoryData
                    .OrderByDescending(kv => kv.Key)
                    .Take(numberOfTelemetries)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            return Task.FromResult(telemetryList);
        }

        public void SetConfiguration(string config)
        {
            try
            {
                var configData = JsonConvert.DeserializeObject<DeviceConfiguration>(config);
                if (configData != null)
                    EntityConfig = configData;
            }
            catch (Exception)
            {
            }
        }
        #endregion [ Behaviour ]

        #region [ Private Methods ]
        private void CheckAlert()
        {
            if (LastData.Telemetries.ContainsKey("temperature"))
            {
                var lastTemperature = LastData.Telemetries["temperature"];
                if (EntityConfig.TemperatureHighAlertEnabled())
                {
                    if (!TemperatureHighNotificationFired &&
                        lastTemperature > EntityConfig.TemperatureHighThreshold)
                    {
                        SendAlert(lastTemperature);
                        TemperatureHighNotificationFired = true;
                    }
                    if (lastTemperature <= EntityConfig.TemperatureHighThreshold)
                    {
                        TemperatureHighNotificationFired = false;
                    }
                }

                if (EntityConfig.TemperatureLowAlertEnabled())
                {
                    if (!TemperatureLowNotificationFired &&
                        lastTemperature < EntityConfig.TemperatureLowThreshold)
                    {
                        SendAlert(lastTemperature);
                        TemperatureLowNotificationFired = true;
                    }
                    if (lastTemperature >= EntityConfig.TemperatureLowThreshold)
                    {
                        TemperatureLowNotificationFired = false;
                    }
                }
            }
        }

        private void SendAlert(double lastTemperature)
        {
            var notificationEntityId = new EntityId(nameof(DeviceNotificationsEntity),
                $"{Entity.Current.EntityName}|{Entity.Current.EntityKey}");

            var notification = new DeviceNotificationInfo()
            {
                Timestamp = DateTimeOffset.Now,
                DeviceId= Entity.Current.EntityKey,
                DeviceType= Entity.Current.EntityName
            };
            notification.Telemetries.Add("temperature", lastTemperature);
            notification.Metadata.Add("notificationNumber", EntityConfig?.NotificationNumber);

            Entity.Current.SignalEntity<IDeviceNotificationEntity>(notificationEntityId,
                n => n.NotificationFired(notification));

            if (!string.IsNullOrWhiteSpace(EntityConfig?.NotificationNumber))
            {
                Entity.Current.StartNewOrchestration("Alerts_SendTemperatureNotification",
                                            new Alerts.TemperatureAlerts.TemperatureNotificationData()
                                            {
                                                DeviceName = DeviceName,
                                                NotificationNumber = EntityConfig.NotificationNumber,
                                                Temperature = lastTemperature
                                            });
            }
        }

        private void ClearHistoryData()
        {
            var dataToRemove = HistoryData
                 .Where(a => a.Key < DateTimeOffset.Now.Subtract(EntityConfig.HistoryRetention));

            if (dataToRemove.Any())
            {
                foreach (var item in dataToRemove)
                {
                    HistoryData.Remove(item.Key);
                }
            }
        }
        #endregion [ Private Methods ]

        [FunctionName(nameof(TemperatureDeviceEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger logger)
            => ctx.DispatchAsync<TemperatureDeviceEntity>(logger);
    }

}
