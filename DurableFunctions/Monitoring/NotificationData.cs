using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Monitoring
{
    public class NotificationData
    {
        public string PhoneNumber { get; set; }

        public string SmsMessage { get; set; }

        public string FromPhoneNumber { get; set; }

        public override string ToString()
        {
            return $"phoneNumber={PhoneNumber}, fromPhoneNumber={FromPhoneNumber}, smsMessage={SmsMessage}";
        }
    }
}
