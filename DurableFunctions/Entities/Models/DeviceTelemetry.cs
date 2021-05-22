using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Entities.Models
{
    public class DeviceTelemetry
    {
        [JsonProperty("deviceType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceType Type { get; set; }
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
        [JsonProperty("data")]
        public DeviceData Data { get; set; }

    }

}
