using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Entities.Models
{
    public class DeviceNotificationInfo
    {
        public string DeviceType { get; set; }
        public string DeviceId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Dictionary<string, double> Telemetries { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
