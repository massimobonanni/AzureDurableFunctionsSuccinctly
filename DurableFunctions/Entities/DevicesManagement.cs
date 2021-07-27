using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Entities.Models;
using DurableFunctions.Entities.TemperatureDevice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DurableFunctions.Entities
{
    public class DevicesManagement
    {
        private readonly IEntityFactory _entityfactory;

        public DevicesManagement(IEntityFactory entityFactory)
        {
            _entityfactory = entityFactory;
        }

        [FunctionName(nameof(SendTelemetryToDevice))]
        public async Task<IActionResult> SendTelemetryToDevice(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "devices/{deviceId}/telemetries")] HttpRequest req,
            string deviceId,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var telemetry = JsonConvert.DeserializeObject<DeviceTelemetry>(requestBody);
            telemetry.DeviceId = deviceId;

            var entityId = await _entityfactory.GetEntityIdAsync(telemetry.DeviceId, telemetry.Type, default);

            await client.SignalEntityAsync<IDeviceEntity>(entityId, d => d.TelemetryReceived(telemetry));

            return new OkObjectResult(telemetry);
        }

        [FunctionName(nameof(GetDevices))]
        public async Task<IActionResult> GetDevices(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices")] HttpRequest req,
          [DurableClient] IDurableEntityClient client)
        {
            if (!Enum.TryParse(typeof(DeviceType), req.Query["deviceType"], true, out var deviceType))
            {
                return new BadRequestResult();
            }

            var result = new List<DeviceInfoModel>();

            EntityQuery queryDefinition = new EntityQuery()
            {
                PageSize = 100,
                FetchState = true,

            };
            queryDefinition.EntityName = await _entityfactory.GetEntityNameAsync((DeviceType)deviceType, default);

            do
            {
                EntityQueryResult queryResult = await client.ListEntitiesAsync(queryDefinition, default);

                foreach (var item in queryResult.Entities)
                {
                    DeviceInfoModel model = item.ToDeviceInfoModel();
                    // if you want to add other filters to you method
                    // you can add them here before adding the model to the return list
                    result.Add(model);
                }

                queryDefinition.ContinuationToken = queryResult.ContinuationToken;
            } while (queryDefinition.ContinuationToken != null);

            return new OkObjectResult(result);
        }

        [FunctionName(nameof(GetDevice))]
        public async Task<IActionResult> GetDevice(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices/{deviceId}")] HttpRequest req,
            string deviceId,
            [DurableClient] IDurableEntityClient client)
        {
            if (!Enum.TryParse(typeof(DeviceType), req.Query["deviceType"], true, out var deviceType))
            {
                return new BadRequestObjectResult(deviceId);
            }

            EntityId entityId = await _entityfactory.GetEntityIdAsync(deviceId, (DeviceType)deviceType, default);

            EntityStateResponse<JObject> entity = await client.ReadEntityStateAsync<JObject>(entityId);
            if (entity.EntityExists)
            {
                var device = entity.EntityState.ToDeviceDetailModel();
                device.DeviceId = deviceId;
                return new OkObjectResult(device);
            }
            return new NotFoundObjectResult(deviceId);
        }

        [FunctionName(nameof(SetConfiguration))]
        public async Task<IActionResult> SetConfiguration(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "devices/{deviceId}/configuration")] HttpRequest req,
            string deviceId,
            [DurableClient] IDurableEntityClient client)
        {
            if (!Enum.TryParse(typeof(DeviceType), req.Query["deviceType"], true, out var deviceType))
            {
                return new BadRequestObjectResult(deviceId);
            }

            EntityId entityId = await _entityfactory.GetEntityIdAsync(deviceId, (DeviceType)deviceType, default);

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            await client.SignalEntityAsync<IDeviceEntity>(entityId, d => d.SetConfiguration(requestBody));

            return new OkObjectResult(requestBody);
        }

        [FunctionName(nameof(GetDeviceNotifications))]
        public async Task<IActionResult> GetDeviceNotifications(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications")] HttpRequest req,
          [DurableClient] IDurableEntityClient client)
        {
            var result = new List<JObject>();

            EntityQuery queryDefinition = new EntityQuery()
            {
                PageSize = 100,
                FetchState = true,
                EntityName = nameof(DeviceNotificationsEntity)
            };

            do
            {
                EntityQueryResult queryResult = await client.ListEntitiesAsync(queryDefinition, default);

                foreach (var item in queryResult.Entities)
                {
                    result.Add(item.State as JObject);
                }

                queryDefinition.ContinuationToken = queryResult.ContinuationToken;
            } while (queryDefinition.ContinuationToken != null);

            return new OkObjectResult(result);
        }
    }

}
