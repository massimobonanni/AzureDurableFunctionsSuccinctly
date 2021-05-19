using DurableFunctions.FunctionChaining;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class TextReaderExtensions
    {
        public static async Task<char[]> ReadBufferAsync(this TextReader reader)
        {
            List<char> returnArray = new List<char>();

            char[] buffer = new char[1024];
            var index = 0;
            int count = 0;
            do
            {
                count = await reader.ReadBlockAsync(buffer, index, 1024);
                index += count;
                returnArray.AddRange(buffer.Take(count));
            } while (count == 1024);

            return returnArray.ToArray();
        }
    }
}
