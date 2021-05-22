using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DurableFunctions.Helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace DurableFunctions.FunctionChaining
{
    public class FunctionChainingFunctions
    {
        [FunctionName("OrderManager_Client")]
        public async Task<HttpResponseMessage> Client(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "functionchaining/order")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT OrderManager_Client] --> Order received!");
            string jsonContent = await req.Content.ReadAsStringAsync();

            try
            {
                var order = JsonConvert.DeserializeObject<Order>(jsonContent);
                var instanceId = await starter.StartNewAsync("OrderManager_Orchestrator", order);

                log.LogInformation($"Order received - started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, instanceId);
            }
            catch (Exception ex)
            {
                log.LogError("Error during order received operation", ex);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        [FunctionName("OrderManager_Orchestrator")]
        public async Task<Invoice> Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation($"[ORCHESTRATOR OrderManager_Orchestrator] --> id : {context.InstanceId}");

            var order = context.GetInput<Order>();

            var orderRow = await context.CallActivityAsync<OrderRow>("OrderManager_SaveOrder", order);

            var invoice = await context.CallActivityAsync<Invoice>("OrderManager_CreateInvoice", orderRow);

            await context.CallActivityAsync("OrderManager_SendMail", invoice);

            return invoice;
        }

        [FunctionName("OrderManager_SaveOrder")]
        public async Task<OrderRow> SaveOrder([ActivityTrigger] Order order,
            [Table("ordersTable", Connection = "StorageAccount")] IAsyncCollector<OrderRow> ordersTable,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY OrderManager_SaveOrder] --> order : {order}");

            var orderRow = new OrderRow(order);
            await ordersTable.AddAsync(orderRow);
            return orderRow;

        }

        [FunctionName("OrderManager_CreateInvoice")]
        [StorageAccount("StorageAccount")]
        public async Task<Invoice> CreateInvoice([ActivityTrigger] OrderRow order,
            IBinder outputBinder,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY OrderManager_CreateInvoice] --> order : {order.orderId}");

            var fileName = $"invoices/{order.orderId}.txt";

            using (var outputBlob = outputBinder.Bind<TextWriter>(new BlobAttribute(fileName)))
            {
                await outputBlob.WriteInvoiceAsync(order);
            }

            var invoice = new Invoice() { order = order, fileName = $"{order.orderId}.txt" };

            return invoice;
        }

        [FunctionName("OrderManager_SendMail")]
        [StorageAccount("StorageAccount")]
        public async Task SendMail([ActivityTrigger] Invoice invoice,
             [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
             IBinder invoiceBinder,
             ILogger log)
        {
            log.LogInformation($"[ACTIVITY OrderManager_SendMail] --> invoice : {invoice}");

            SendGridMessage message;
            using (var inputBlob = invoiceBinder.Bind<TextReader>(new BlobAttribute(invoice.fileName)))
            {
                message = await SendGridHelper.CreateMessageAsync(invoice, inputBlob);
            }
            await messageCollector.AddAsync(message);
        }
    }
}