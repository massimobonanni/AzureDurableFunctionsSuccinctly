using DurableFunctions.Entities.Models;

namespace DurableFunctions.Entities.Interfaces
{
    public interface IDeviceEntity
    {
        void SetConfiguration(string config);
        void TelemetryReceived(DeviceTelemetry telemetry);
    }
}