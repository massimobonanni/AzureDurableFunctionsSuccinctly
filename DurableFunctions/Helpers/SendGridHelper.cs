using DurableFunctions.FunctionChaining;
using Microsoft.Azure.WebJobs;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Helpers
{
    public static class SendGridHelper
    {
        public static async Task<SendGridMessage> CreateMessageAsync(Invoice invoice,TextReader invoiceReader)
        {
            var message = new SendGridMessage()
            {
                Subject = "Azure Functions Invoice",
                From = new EmailAddress("azureinvoice@invoiceplatform.com")
            };
            message.AddTo(new EmailAddress(invoice.order.custEmail));

            var buffer = await invoiceReader.ReadBufferAsync();

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(buffer);
            var text = System.Convert.ToBase64String(plainTextBytes);

            message.AddContent("text/plain", System.Text.Encoding.UTF8.GetString(plainTextBytes));
            message.AddAttachment(invoice.fileName, text, "text/plain", "attachment", "Invoice File");

            return message;
        }

    }
}
