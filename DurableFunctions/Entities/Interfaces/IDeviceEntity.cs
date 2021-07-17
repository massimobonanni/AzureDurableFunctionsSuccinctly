using DurableFunctions.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableFunctions.Entities.Interfaces
{
    public interface IDeviceEntity
    {
        void SetConfiguration(string config);
        void TelemetryReceived(DeviceTelemetry telemetry);
        Task<IDictionary<DateTimeOffset, DeviceData>> GetLastTelemetries(int numberOfTelemetries = 10);
    }
}