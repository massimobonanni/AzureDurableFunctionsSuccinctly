using DurableFunctions.FunctionChaining;
using DurableFunctions.HumanInteraction;
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

        public static SendGridMessage CreateMessageForManager(VacationResponse response)
        {
            var message = new SendGridMessage()
            {
                Subject = $"Vacation Request for {response.request.employeeFirstName} {response.request.employeeLastName}",
                From = new EmailAddress("noreply@vacationplatform.com")
            };
            message.AddTo(new EmailAddress(response.request.managerEmail));

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine($"<p>{response.request.employeeFirstName } {response.request.employeeLastName} request a vacation '{response.request.notes}'<br/>");
            msgBuilder.AppendLine($"from {response.request.dateFrom } to {response.request.dateTo}.</p>");
            msgBuilder.AppendLine($"<br/><br/>");
            msgBuilder.AppendLine($"<p>Please use the code <b>{response.instanceId}</b> to accept or reject the request.");
            message.HtmlContent = msgBuilder.ToString();

            return message;
        }

        public static SendGridMessage CreateMessageForEmployee(VacationResponse response)
        {
            var message = new SendGridMessage()
            {
                Subject = $"Your Vacation Request from {response.request.dateFrom:dd/MM/yyyy} to {response.request.dateTo:dd/MM/yyyy}",
                From = new EmailAddress("noreply@vacationplatform.com")
            };
            message.AddTo(new EmailAddress(response.request.employeeEmail));

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine($"<p>Hi, {response.request.employeeFirstName } <br/> your vacation request '{response.request.notes}'");
            msgBuilder.AppendLine($"from {response.request.dateFrom:dd/MM/yyyy} to {response.request.dateTo:dd/MM/yyyy} is ");
            if (response.isApproved.HasValue && response.isApproved.Value)
            {
                msgBuilder.AppendLine($"<b>Approved</b>");
            }
            else
            {
                msgBuilder.AppendLine($"<b>Rejected</b>");
            }
            msgBuilder.AppendLine($"</p>");
            message.HtmlContent = msgBuilder.ToString();

            return message;
        }
    }
}
