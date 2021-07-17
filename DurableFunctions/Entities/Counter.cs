using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Entities
{
    public static class FunctionBasedEntity
    {
        [FunctionName("CounterFunctionBased")]
        public static void Counter([EntityTrigger] IDurableEntityContext ctx)
        {
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "add":
                    ctx.SetState(ctx.GetState<int>() + ctx.GetInput<int>());
                    break;
                case "reset":
                    ctx.SetState(0);
                    break;
                case "get":
                    ctx.Return(ctx.GetState<int>());
                    break;
            }
        }
    }
    public interface IMonitorEntity
    {
        void MethodCalled(string methodName);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MonitorEntity : IMonitorEntity
    {
        [JsonProperty("methodsCounters")]
        public Dictionary<string, int> MethodsCounters { get; set; }

        public void MethodCalled(string methodName)
        {
            if (MethodsCounters == null)
                MethodsCounters = new Dictionary<string, int>();

            if (MethodsCounters.ContainsKey(methodName))
                MethodsCounters[methodName]++;
            else
                MethodsCounters[methodName] = 1;
        }

        [FunctionName(nameof(MonitorEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
                => ctx.DispatchAsync<MonitorEntity>();
    }

    public interface ICounter
    {
        void Add(int amount);
        Task AddAsync(int amount);
        Task ResetAsync();
        Task<int> GetAsync();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Counter : ICounter
    {
        [JsonProperty("value")]
        public int CurrentValue { get; set; }

        public void Add(int amount) => this.CurrentValue += amount;

        public Task AddAsync(int amount)
        {
            this.Add(amount);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            this.CurrentValue = 0;

            // Monitoring all the reset operation signaling to a MonitorEntity
            // the monitoring entity has the same key of the counter
            var monitorEntityId = new EntityId("MonitorEntity", Entity.Current.EntityId.ToString());
            Entity.Current.SignalEntity<IMonitorEntity>(monitorEntityId, m => m.MethodCalled("ResetAsync"));

            return Task.CompletedTask;
        }

        public Task<int> GetAsync() => Task.FromResult(this.CurrentValue);

        [FunctionName(nameof(Counter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Counter>();
    }

    public static class OtherFunctions
    {
        [FunctionName("AddFromHttp")]
        public static async Task<IActionResult> AddFromHttp(
            [HttpTrigger(AuthorizationLevel.Function, Route = "counters/{counterName}/add")] HttpRequest req,
            string counterName,
            [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(Counter), counterName);
            int amount = int.Parse(req.Query["value"]);
            await client.SignalEntityAsync(entityId, "Add", amount);
            return new OkObjectResult(counterName);
        }

        [FunctionName("ResetFromHttp")]
        public static async Task<IActionResult> ResetFromHttp(
            [HttpTrigger(AuthorizationLevel.Function, Route = "counters/{counterName}/reset")] HttpRequest req,
            string counterName,
            [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(Counter), counterName);
            await client.SignalEntityAsync(entityId, "ResetAsync", null);
            return new OkObjectResult(counterName);
        }

        [FunctionName("AddFromQueue")]
        public static Task AddFromQueue([QueueTrigger("durable-function-trigger")] string input,
            [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(Counter), "myCounter");
            int amount = int.Parse(input);
            return client.SignalEntityAsync(entityId, "Add", amount);
        }

        [FunctionName("AddFromQueueWithInterface")]
        public static Task AddFromQueueWithInterface([QueueTrigger("durable-function-trigger")] string input,
            [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(Counter), "myCounter");
            int amount = int.Parse(input);
            return client.SignalEntityAsync<ICounter>(entityId, c => c.Add(amount));
        }

        public class CounterQueryState
        {
            public string Key { get; set; }
            public int Value { get; set; }
        }

        public class EntityQueryState
        {
            public string Key { get; set; }
            public JObject State { get; set; }
        }

        [FunctionName("EntityList")]
        public static async Task<IActionResult> EntityList(
                [HttpTrigger(AuthorizationLevel.Function, Route = "entities")] HttpRequest req,
                [DurableClient] IDurableEntityClient client)
        {
            var entityName = req.Query["entityName"];
            var responseList = new List<EntityQueryState>();

            var queryDefinition = new EntityQuery()
            {
                PageSize = 100,
                FetchState = true,
            };
            if (!string.IsNullOrWhiteSpace(entityName))
                queryDefinition.EntityName = entityName;

            do
            {
                EntityQueryResult queryResult = await client.ListEntitiesAsync(queryDefinition, default);

                foreach (var item in queryResult.Entities)
                {
                    var entityState = new EntityQueryState()
                    {
                        State = item.State as JObject,
                        Key = item.EntityId.EntityKey
                    };
                    responseList.Add(entityState);
                }

                queryDefinition.ContinuationToken = queryResult.ContinuationToken;
            } while (queryDefinition.ContinuationToken != null);

            return new OkObjectResult(responseList);
        }

        [FunctionName("CounterList")]
        public static async Task<IActionResult> CounterList(
                [HttpTrigger(AuthorizationLevel.Function, Route = "counters")] HttpRequest req,
                [DurableClient] IDurableEntityClient client)
        {
            var responseList = new List<CounterQueryState>();

            var queryDefinition = new EntityQuery()
            {
                PageSize = 100,
                FetchState = true,
                EntityName = nameof(Counter)
            };

            do
            {
                EntityQueryResult queryResult = await client.ListEntitiesAsync(queryDefinition, default);

                foreach (var item in queryResult.Entities)
                {
                    var counterState = item.State.ToObject<CounterQueryState>();
                    counterState.Key = item.EntityId.EntityKey;
                    responseList.Add(counterState);
                }

                queryDefinition.ContinuationToken = queryResult.ContinuationToken;
            } while (queryDefinition.ContinuationToken != null);

            return new OkObjectResult(responseList);
        }

        [FunctionName("GetCounterState")]
        public static async Task<IActionResult> GetCounterState(
                [HttpTrigger(AuthorizationLevel.Function, Route = "counters/{counterName}")] HttpRequest req,
                string counterName,
                [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(Counter), counterName);
            EntityStateResponse<JObject> stateResponse = await client.ReadEntityStateAsync<JObject>(entityId);
            return new OkObjectResult(stateResponse.EntityState);
        }

        [FunctionName("CounterOrchestration")]
        public static async Task CounterOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var counterName = context.GetInput<string>();
            var entityId = new EntityId(nameof(Counter), counterName);

            int currentValue = await context.CallEntityAsync<int>(entityId, "Get");
            if (currentValue < 10)
            {
                context.SignalEntity(entityId, "Add", 1);
            }
        }

        [FunctionName("CounterOrchestrationWithProxy")]
        public static async Task CounterOrchestrationWithProxy([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var counterName = context.GetInput<string>();
            var entityId = new EntityId(nameof(Counter), counterName);

            var entityProxy = context.CreateEntityProxy<ICounter>(entityId);

            int currentValue = await entityProxy.GetAsync();
            if (currentValue < 10)
            {
                entityProxy.Add(1);
            }
        }


        [FunctionName("AddCounterValueClient")]
        public static async Task<IActionResult> AddCounterValueClient(
                [HttpTrigger(AuthorizationLevel.Function)] HttpRequest req,
                [DurableClient] IDurableOrchestrationClient client)
        {
            var sourceCounterName = req.Query["source"];
            var destCounterName = req.Query["dest"];
            if (string.IsNullOrWhiteSpace(sourceCounterName) &&
                string.IsNullOrWhiteSpace(destCounterName))
            {
                return new BadRequestResult();
            }

            var orchestrationId = await client.StartNewAsync<string>("AddCounterValue", $"{sourceCounterName}|{destCounterName}");
            return client.CreateCheckStatusResponse(req, orchestrationId);
        }

        [FunctionName("AddCounterValue")]
        public static async Task AddCounterValue([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var counters = context.GetInput<string>();
            if (string.IsNullOrWhiteSpace(counters))
                return;
            var counterNames = counters.Split("|");
            if (counterNames.Count() != 2)
                return;

            var sourceEntityId = new EntityId(nameof(Counter), counterNames[0]);
            var destEntityId = new EntityId(nameof(Counter), counterNames[1]);

            using (await context.LockAsync(sourceEntityId, destEntityId))
            {
                ICounter sourceProxy = context.CreateEntityProxy<ICounter>(sourceEntityId);
                ICounter destProxy = context.CreateEntityProxy<ICounter>(destEntityId);

                var sourceValue = await sourceProxy.GetAsync();

                await destProxy.AddAsync(sourceValue);
                await sourceProxy.AddAsync(-sourceValue);
            }
        }
    }
}
