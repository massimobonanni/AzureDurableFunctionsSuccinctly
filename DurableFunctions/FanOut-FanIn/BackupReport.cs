namespace DurableFunctions.FanOutFanIn
{
    public class BackupReport
    {
        public int NumberOfFiles { get;  set; }
        public object TotalBytes { get; internal set; }
    }
}