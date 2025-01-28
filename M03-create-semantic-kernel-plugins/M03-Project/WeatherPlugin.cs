using System.ComponentModel;
using Microsoft.SemanticKernel;

class WeatherPlugin
{
    // Simulated weather data
    private static readonly Dictionary<string, WeatherData> mockWeatherData = new Dictionary<string, WeatherData>
    {
        { "London", new WeatherData(12.5f, 75, "cloudy") },
        { "New York", new WeatherData(22.1f, 60, "sunny") },
        { "Tokyo", new WeatherData(18.3f, 85, "rainy") },
        { "Osaka", new WeatherData(22.1f, 60, "sunny") },
        { "Paris", new WeatherData(14.0f, 70, "partly cloudy") },
        { "Sydney", new WeatherData(25.0f, 50, "sunny") }
    };

    // Weather data model
    public class WeatherData
    {
        public float Temperature { get; set; }
        public int Humidity { get; set; }
        public string Condition { get; set; }

        public WeatherData(float temperature, int humidity, string condition)
        {
            Temperature = temperature;
            Humidity = humidity;
            Condition = condition;
        }
    }

    [KernelFunction("get_weather")]
    [Description("Gets the current weather details for a city")]
    public static string GetWeather(string city)
    {
        if (mockWeatherData.ContainsKey(city))
        {
            WeatherData data = mockWeatherData[city];
            return $"Weather forecast for {city}:\n" +
                   $"Temperature: {data.Temperature}°C\n" +
                   $"Humidity: {data.Humidity}%\n" +
                   $"Condition: {data.Condition}";
        }
        else
        {
            return $"Sorry, we do not have weather data for {city}.";
        }
    }
}