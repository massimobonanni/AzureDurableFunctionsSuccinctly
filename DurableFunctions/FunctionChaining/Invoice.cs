using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.FunctionChaining
{
    public class Invoice
    {
        public OrderRow order { get; set; }

        public string fileName { get; set; }

        public override string ToString()
        {
            return $"order=[{order}], fileName={fileName}";
        }
    }
}
