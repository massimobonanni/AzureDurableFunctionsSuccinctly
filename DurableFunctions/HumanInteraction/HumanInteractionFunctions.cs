using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DurableFunctions.Helpers;
using DurableFunctions.Monitoring;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace DurableFunctions.HumanInteraction
{
    public class HumanInteractionFunctions
    {

        private readonly IConfiguration configuration;

        public HumanInteractionFunctions(IWeatherService weatherService, IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.configuration = configuration;
        }

        [FunctionName("HumanInteraction_Client")]
        public async Task<HttpResponseMessage> Client(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "humaninteraction/vacationrequest")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT HumanInteraction_Client] --> Vacation requested!");
            string jsonContent = await req.Content.ReadAsStringAsync();

            try
            {
                var vacationRequest = JsonConvert.DeserializeObject<VacationRequest>(jsonContent);
                if (vacationRequest.IsValid())
                {
                    var instanceId = await starter.StartNewAsync("HumanInteraction_Orchestrator", vacationRequest);
                    log.LogInformation($"Monitor started - started orchestration with ID = '{instanceId}'.");
                    return starter.CreateCheckStatusResponse(req, instanceId);
                }
            }
            catch (Exception ex)
            {
                log.LogError("Error during requesting vacation", ex);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        [FunctionName("HumanInteraction_ClientApprove")]
        public async Task<HttpResponseMessage> ClientApprove(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "humaninteraction/vacationrequest/{instanceId}/approve")] HttpRequestMessage req,
            string instanceId, [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT HumanInteraction_ClientApprove] --> instanceId {instanceId} approved");
            await starter.RaiseEventAsync(instanceId, RequestEvents.Approved, null);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        [FunctionName("HumanInteraction_ClientReject")]
        public async Task<HttpResponseMessage> ClientReject(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "humaninteraction/vacationrequest/{instanceId}/reject")] HttpRequestMessage req,
            string instanceId, [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT HumanInteraction_ClientReject] --> instanceId {instanceId} rejected");
            await starter.RaiseEventAsync(instanceId, RequestEvents.Rejected, null);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        [FunctionName("HumanInteraction_Orchestrator")]
        public async Task Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation($"[ORCHESTRATOR HumanInteraction_Orchestrator] --> id : {context.InstanceId}");

            var request = context.GetInput<VacationRequest>();
            var response = new VacationResponse()
            {
                request = request,
                instanceId = context.InstanceId
            };

            await context.CallActivityAsync("HumanInteraction_SendMailToManager", response);

            using (var timeoutCts = new CancellationTokenSource())
            {
                DateTime expiration = context.CurrentUtcDateTime.AddDays(1);
                Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                Task approvedResponseTask = context.WaitForExternalEvent(RequestEvents.Approved);
                Task rejectedResponseTask = context.WaitForExternalEvent(RequestEvents.Rejected);

                var taskCompleted = await Task.WhenAny(timeoutTask, approvedResponseTask, rejectedResponseTask);

                if (taskCompleted == approvedResponseTask || taskCompleted == timeoutTask) // request approved
                {
                    response.isApproved = true;
                    await context.CallActivityAsync("HumanInteraction_SaveRequest", response);
                }
                else
                {
                    response.isApproved = false;
                }

                await context.CallActivityAsync("HumanInteraction_SendMailToEmployee", response);
            }

        }

        [FunctionName("HumanInteraction_SendMailToManager")]
        public async Task SendMailToManager([ActivityTrigger] VacationResponse response,
            [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY HumanInteraction_SendMailToManager] --> response : {response}");
            var message = SendGridHelper.CreateMessageForManager(response);
            await messageCollector.AddAsync(message);
         }

        [FunctionName("HumanInteraction_SendMailToEmployee")]
        public async Task SendMailToEmployee([ActivityTrigger] VacationResponse response,
            [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY HumanInteraction_SendMailToEmployee] --> response : {response}");
            var message = SendGridHelper.CreateMessageForEmployee(response);
            await messageCollector.AddAsync(message);

        }

        [FunctionName("HumanInteraction_SaveRequest")]
        public async Task<bool> SaveRequest([ActivityTrigger] VacationResponse response,
            [Table("vacationsTable", Connection = "StorageAccount")] IAsyncCollector<VacationResponseRow> vacationsTable,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY HumanInteraction_SaveRequest] --> response : {response}");

            var vacationRow = new VacationResponseRow(response);
            await vacationsTable.AddAsync(vacationRow);

            return true;
        }
    }
}