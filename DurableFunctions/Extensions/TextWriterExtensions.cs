using DurableFunctions.FunctionChaining;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class TextWriterExtensions
    {
        public static async Task WriteInvoiceAsync(this TextWriter writer, OrderRow order)
        {
            await writer.WriteLineAsync($"Invoice generated at {DateTime.Now} for order {order.orderId} created at {order.date}");
            await writer.WriteLineAsync($"");
            await writer.WriteLineAsync($"Customer : {order.custName}");
            await writer.WriteLineAsync($"Address: {order.custAddress}");
            await writer.WriteLineAsync($"Email: {order.custEmail}");
            await writer.WriteLineAsync($"");
            await writer.WriteLineAsync($"Total : {order.price}€");
        }
    }
}
