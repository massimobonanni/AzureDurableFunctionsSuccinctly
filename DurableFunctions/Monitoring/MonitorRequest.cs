namespace DurableFunctions.Monitoring
{
    public class MonitorRequest
    {
        public string city { get; set; }

        public int durationInMinutes { get; set; } = 60;

        public int pollingInMinutes { get; set; } = 10;

        public string weatherConditionCheck { get; set; } = "clear sky";

        public string phoneNumber { get; set; }

        public override string ToString()
        {
            return $"city={city}, weatherConditionCheck={weatherConditionCheck}";
        }
    }
}