using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Entities.Models
{
    public class DeviceInfoModel
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }
        [JsonProperty("lastUpdate")]
        public DateTimeOffset LastUpdate { get; set; }

        [JsonProperty("lastTelemetries")]
        public Dictionary<string, double> LastTelemetries { get; set; }
    }
}
