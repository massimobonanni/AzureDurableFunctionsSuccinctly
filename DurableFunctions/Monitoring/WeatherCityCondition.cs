using System;
using System.Collections.Generic;
using System.Text;

namespace DurableFunctions.Monitoring
{
    public class WeatherCityCondition
    {

        public string CityCode { get; set; }

        public string Weather { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        internal bool HasCondition(string weatherConditionCheck)
        {
            if (string.IsNullOrWhiteSpace(Weather)) 
                return false;
            if (string.IsNullOrWhiteSpace(weatherConditionCheck))
                return false;
            return Weather.ToLower()
                == weatherConditionCheck.ToLower();
        }
    }
}
