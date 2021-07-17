using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DurableFunctions.FunctionChaining
{
    public class Invoice
    {
        public OrderRow order { get; set; }

        public string fullPath { get; set; }

        public override string ToString()
        {
            return $"order=[{order}], fullPath={fullPath}";
        }

        public string GetFileName()
        {
            if (fullPath == null) return null;

            return fullPath.Split('/').LastOrDefault();
        }
    }
}
