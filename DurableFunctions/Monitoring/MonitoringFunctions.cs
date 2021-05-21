using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DurableFunctions.Helpers;
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

namespace DurableFunctions.Monitoring
{
    public class MonitoringFunctions
    {
        private readonly IWeatherService weatherService;
        private readonly IConfiguration configuration;

        public MonitoringFunctions(IWeatherService weatherService, IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));

            this.configuration = configuration;
            this.weatherService.ApiKey = configuration.GetValue<string>("WeatherServiceAPI");
        }

        [FunctionName("Monitoring_Client")]
        public async Task<HttpResponseMessage> Client(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "monitoring/monitor")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT Monitoring_Client] --> Monitor started!");
            string jsonContent = await req.Content.ReadAsStringAsync();

            try
            {
                var monitorRequest = JsonConvert.DeserializeObject<MonitorRequest>(jsonContent);
                var instanceId = await starter.StartNewAsync("Monitoring_Orchestrator", monitorRequest);

                log.LogInformation($"Monitor started - started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, instanceId);
            }
            catch (Exception ex)
            {
                log.LogError("Error during starting monitor orchestrator", ex);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        [FunctionName("Monitoring_Orchestrator")]
        public  async Task Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation($"[ORCHESTRATOR Monitoring_Orchestrator] --> id : {context.InstanceId}");

            var request = context.GetInput<MonitorRequest>();

            DateTime endTime = context.CurrentUtcDateTime.AddMinutes(request.durationInMinutes);

            while (context.CurrentUtcDateTime < endTime)
            {
                bool isCondition = await context.CallActivityAsync<bool>("Monitoring_WeatherCheck", request);

                if (isCondition)
                {
                    var notificationData = new NotificationData()
                    {
                        FromPhoneNumber = this.configuration.GetValue<string>("TwilioFromNumber"),
                        PhoneNumber = request.phoneNumber,
                        SmsMessage = $"Notification of weather {request.weatherConditionCheck} for city {request.city}"
                    };
                    await context.CallActivityAsync("Monitoring_SendAlert", notificationData);
                    break;
                }
                else
                {
                    var nextCheckpoint = context.CurrentUtcDateTime.AddMinutes(request.pollingInMinutes);
                    await context.CreateTimer(nextCheckpoint, CancellationToken.None);
                }
            }
        }

        [FunctionName("Monitoring_WeatherCheck")]
        public async Task<bool> WeatherCheck([ActivityTrigger] MonitorRequest request,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY Monitoring_WeatherCheck] --> request : {request}");
            try
            {
                var cityCondition = await this.weatherService.GetCityInfoAsync(request.city);
                if (cityCondition != null)
                    return cityCondition.HasCondition(request.weatherConditionCheck);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error during calling weather service");
            }
            return false;
        }

        [FunctionName("Monitoring_SendAlert")]
        [return: TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken")]
        public CreateMessageOptions SendAlert([ActivityTrigger] NotificationData notificationData,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY Monitoring_SendAlert] --> notificationData : {notificationData}");

            var message = new CreateMessageOptions(new PhoneNumber(notificationData.PhoneNumber))
            {
                From = new PhoneNumber(notificationData.FromPhoneNumber),
                Body = notificationData.SmsMessage
            };

            return message;
        }
    }
}