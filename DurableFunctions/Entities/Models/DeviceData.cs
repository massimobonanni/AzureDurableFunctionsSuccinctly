using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Entities.Models
{
    public class DeviceData
    {
        [JsonProperty("telemetries")]
        public Dictionary<string, double> Telemetries { get; set; } = new Dictionary<string, double>();
    }
}
