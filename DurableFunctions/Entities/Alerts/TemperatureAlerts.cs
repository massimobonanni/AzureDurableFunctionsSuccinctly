using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Entities.Alerts
{
    public class TemperatureAlerts
    {
        public class TemperatureNotificationData
        {
            public string NotificationNumber { get; set; }

            public string DeviceName { get; set; }

            public double Temperature { get; set; }
        }

        [FunctionName("Alerts_SendTemperatureNotification")]
        public async Task SendTemperatureNotification(
                [OrchestrationTrigger] IDurableOrchestrationContext context,
                ILogger logger)
        {
            var notificationdata = context.GetInput<TemperatureNotificationData>();

            var smsData = new TwilioActivities.SmsData()
            {
                Number = notificationdata.NotificationNumber,
                Message = $"The temperature for device {notificationdata.DeviceName} is {notificationdata.Temperature}"
            };

            try
            {
                await context.CallActivityAsync("TwilioActivities_SendSMS", smsData);
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Error during TwilioActivity invocation", smsData);
            }
        }
    }
}
