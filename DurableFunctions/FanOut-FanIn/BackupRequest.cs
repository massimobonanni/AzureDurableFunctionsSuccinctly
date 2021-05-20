using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.FanOutFanIn
{
    public class BackupRequest
    {
        public string path { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(path);
        }
    }
}
