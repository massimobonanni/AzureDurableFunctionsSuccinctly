using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.FunctionChaining
{
    public class Order
    {
        public string custName { get; set; }
        public string custAddress { get; set; }
        public string custEmail { get; set; }
        public string cartId { get; set; }
        public DateTime date { get; set; }
        public double price { get; set; }

        public override String ToString()
        {
            return $"custName={custName}, custAddress={custAddress}, custEmail={custEmail}, cartId={cartId}, date={date}, price={price}";
        }

        public string fileName { get; set; }
    }
}
