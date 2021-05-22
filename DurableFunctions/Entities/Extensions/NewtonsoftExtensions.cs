using DurableFunctions.Entities.Models;
using Newtonsoft.Json.Linq;
using ServerlessIoT.Core;
using ServerlessIoT.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Newtonsoft.Json.Linq
{
    public static class NewtonsoftExtensions
    {
        public static DeviceInfoModel ToDeviceInfoModel(this JObject jobject)
        {
            if (jobject == null)
                return null;

            var retVal = new DeviceInfoModel();
            retVal.DeviceName = (string)jobject.Property("deviceName").Value;
            retVal.DeviceType = (string)jobject.Property("deviceType").Value;
            retVal.LastUpdate = DateTimeOffset.Parse(jobject.Property("lastUpdate").Value.ToString());
            retVal.LastTelemetries = jobject.ToDeviceTelemetryModel();

            return retVal;
        }
        public static Dictionary<string, double> ToDeviceTelemetryModel(this JObject jobject)
        {
            if (jobject == null)
                return null;

            Dictionary<string, double> retVal = null;
            var lastTelemetry = jobject.Property("lastData").Value as JObject;
            if (lastTelemetry != null)
            {
                var telemetries = lastTelemetry.Property("telemetries").Value.ToObject<Dictionary<string, double>>();
                retVal = telemetries;
            }

            return retVal;
        }

        public static DeviceDetailModel ToDeviceDetailModel(this JObject jobject)
        {
            if (jobject == null)
                return null;

            var retVal = new DeviceDetailModel();
            retVal.DeviceName = (string)jobject.Property("deviceName").Value;
            retVal.DeviceType = (string)jobject.Property("deviceType").Value;
            retVal.LastUpdate = DateTimeOffset.Parse(jobject.Property("lastUpdate").Value.ToString());
            retVal.LastTelemetries = jobject.ToDeviceTelemetryModel();
            var historyData = jobject.Property("historyData").Value.ToObject<Dictionary<DateTimeOffset, DeviceData>>();

            if (historyData != null)
            {
                retVal.TelemetryHistory = historyData;
            }

            return retVal;
        }
    }
}
