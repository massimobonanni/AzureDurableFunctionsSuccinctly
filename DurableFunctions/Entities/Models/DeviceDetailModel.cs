using DurableFunctions.Entities.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessIoT.Core.Models
{
    public class DeviceDetailModel : DeviceInfoModel
    {
        [JsonProperty("telemetryHistory")]
        public Dictionary<DateTimeOffset, DeviceData> TelemetryHistory { get; set; }
    }
}
