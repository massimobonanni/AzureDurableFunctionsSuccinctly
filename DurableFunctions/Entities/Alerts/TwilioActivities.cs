using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace DurableFunctions.Entities.Alerts
{
    public class TwilioActivities
    {
        private readonly IConfiguration configuration;

        public TwilioActivities(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.configuration = configuration;
        }

        public class SmsData
        {
            public string Message { get; set; }
            public string Number { get; set; }
        }

        [FunctionName("TwilioActivities_SendSMS")]
        [return: TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken")]
        public CreateMessageOptions SendMessageToTwilio([ActivityTrigger] IDurableActivityContext context, 
            ILogger log)
        {
            SmsData data = context.GetInput<SmsData>();

            log.LogInformation($"Sending message to : {data.Number}");

            var fromNumber = this.configuration.GetValue<string>("TwilioFromNumber");
            
                var message = new CreateMessageOptions(new PhoneNumber(data.Number))
            {
                From = new PhoneNumber(fromNumber),
                Body = data.Message
            };

            return message;
        }
    }
}
