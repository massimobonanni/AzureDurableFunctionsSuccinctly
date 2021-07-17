using DurableFunctions.Entities.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DurableFunctions.Entities.TemperatureDevice
{


    public static class TemperatureDeviceEntityFunctionBased
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
                return TemperatureHighThreshold.HasValue && !string.IsNullOrWhiteSpace(NotificationNumber);
            }

            public bool TemperatureLowAlertEnabled()
            {
                return TemperatureLowThreshold.HasValue && !string.IsNullOrWhiteSpace(NotificationNumber);
            }
        }

        public class State
        {
            public State()
            {
                HistoryData = new Dictionary<DateTimeOffset, DeviceData>();
            }

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
        }

        [FunctionName("TemperatureDeviceEntityFunctionBased")]
        public static void DeviceFunctionBased([EntityTrigger] IDurableEntityContext ctx, ILogger logger)
        {
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "telemetryreceived":
                    var innerState3 = ctx.GetState<State>(() => new State());
                    var telemetry = ctx.GetInput<DeviceTelemetry>();
                    innerState3.DeviceName = telemetry.DeviceName;
                    if (telemetry.Timestamp < DateTimeOffset.Now.Subtract(innerState3.EntityConfig.HistoryRetention))
                        return;
                    if (telemetry.Data != null)
                    {
                        innerState3.HistoryData[telemetry.Timestamp] = telemetry.Data;
                        if (innerState3.LastUpdate < telemetry.Timestamp)
                        {
                            innerState3.LastUpdate = telemetry.Timestamp;
                            innerState3.LastData = telemetry.Data;
                        }
                        // Clear History Data - remove all the telemetries older than retention time
                        // Check Alert - check if some alerts are fired;
                    }
                    ctx.SetState(innerState3);
                    break;
                case "getlasttelemetries":
                    var innerState = ctx.GetState<State>(() => new State());
                    var numberOfTelemetries = ctx.GetInput<int>();
                    var telemetries = innerState.HistoryData
                                                .OrderByDescending(kv => kv.Key)
                                                .Take(numberOfTelemetries)
                                                .ToDictionary(kv => kv.Key, kv => kv.Value);
                    ctx.Return(telemetries);
                    break;
                case "setconfiguration":
                    var innerState2 = ctx.GetState<State>(() => new State());
                    var config = ctx.GetInput<string>();
                    var configData = JsonConvert.DeserializeObject<DeviceConfiguration>(config);
                    innerState2.EntityConfig = configData;
                    ctx.SetState(innerState2);
                    break;
                default:
                    break;
            }
        }
    }
}
