using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessIoT.Core.Models
{
    public class DeviceTelemetryModel
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
        [JsonProperty("temperature")]
        public double Temperature { get; set; }
        [JsonProperty("humidity")]
        public double Humidity { get; set; }
    }
}
