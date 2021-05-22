using DurableFunctions.Entities.Models;
using Newtonsoft.Json.Linq;
using ServerlessIoT.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask
{
    public static class DurableEntityStatusExtensions
    {
        public static DeviceInfoModel ToDeviceInfoModel(this DurableEntityStatus entity)
        {
            if (entity == null)
                return null;

            var retVal = new DeviceInfoModel();

            var jobject = entity.State as JObject;
            if (jobject != null)
            {
                retVal = jobject.ToDeviceInfoModel();
            }
            retVal.DeviceId = entity.EntityId.EntityKey;

            return retVal;
        }
    }
}
