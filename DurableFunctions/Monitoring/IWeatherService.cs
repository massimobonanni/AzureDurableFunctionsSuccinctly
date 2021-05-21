using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Monitoring
{
    public interface IWeatherService
    {
        string ApiKey { get; set; }

        Task<WeatherCityCondition> GetCityInfoAsync(string cityCode);
    }
}
