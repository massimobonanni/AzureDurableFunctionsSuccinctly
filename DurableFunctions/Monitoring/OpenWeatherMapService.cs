using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctions.Monitoring
{
    public class OpenWeatherMapService : IWeatherService
    {
        private string baseUri = "https://api.openweathermap.org/data/2.5/weather";

        public string ApiKey { get; set; }

        private Uri GetApiUrl(string cityCode)
        {
            return new Uri($"{baseUri}?q={cityCode}&appId={ApiKey}&units=metric");
        }

        public async Task<WeatherCityCondition> GetCityInfoAsync(string cityCode)
        {
            string response = null;
            using (var client = new HttpClient())
            {
                response = await client.GetStringAsync(GetApiUrl(cityCode));
            }

            var responseObj = JsonConvert.DeserializeObject<WeatherData>(response);

            WeatherCityCondition city = null;

            if (responseObj.cod == 200)
            {
                city = new WeatherCityCondition()
                {
                    CityCode = cityCode,
                    Weather = responseObj.weather.FirstOrDefault()?.description,
                    Timestamp = responseObj.dt.ToUtcDateTimeOffset()
                };
            }

            return city;
        }
    }

}
