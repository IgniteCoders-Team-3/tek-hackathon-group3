using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.SemanticKernel;
using Modules.Ai.Infrastructure.Weather;

namespace Modules.Ai.Infrastructure.SemanticKernel.Skills;

/// <summary>
/// SK skill that surfaces Open-Meteo weather data to the LLM.
/// The model can invoke these functions when a user asks about weather conditions
/// in any city — particularly useful for correlating weather with network incidents
/// (e.g. storms causing tower outages in Lagos).
/// </summary>
public sealed class WeatherSkill(WeatherService weather)
{
    [KernelFunction("get_current_weather")]
    [Description("Get current weather conditions for a city: temperature, humidity, wind speed, and conditions. Use when the user asks about current weather or when correlating weather with network issues.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("City name, e.g. 'Lagos', 'Ikeja', 'Lekki', 'Abuja'")] string city,
        CancellationToken cancellationToken = default)
    {
        CurrentWeatherDto? current = await weather.GetCurrentWeatherAsync(city, cancellationToken);
        if (current is null)
        {
            return $"Could not retrieve weather data for '{city}'. The city may not be recognized.";
        }

        return new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"Weather for {current.City}, {current.Country}:")
            .AppendLine(CultureInfo.InvariantCulture, $"  Condition: {current.Condition}")
            .AppendLine(CultureInfo.InvariantCulture, $"  Temperature: {current.Temperature}°C")
            .AppendLine(CultureInfo.InvariantCulture, $"  Humidity: {current.Humidity}%")
            .AppendLine(CultureInfo.InvariantCulture, $"  Wind Speed: {current.WindSpeed} km/h")
            .ToString();
    }

    [KernelFunction("get_weather_forecast")]
    [Description("Get a 5-day weather forecast for a city including daily high/low temperatures and conditions. Use when the user asks about upcoming weather or planning maintenance windows around weather.")]
    public async Task<string> GetWeatherForecastAsync(
        [Description("City name, e.g. 'Lagos', 'Ikeja', 'Lekki', 'Abuja'")] string city,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ForecastDayDto> forecast = await weather.GetForecastAsync(city, days: 5, cancellationToken);
        if (forecast.Count == 0)
        {
            return $"Could not retrieve forecast data for '{city}'.";
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"5-day forecast for {city}:");
        foreach (ForecastDayDto day in forecast)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  {day.Date}: {day.Condition}, High {day.TempMax}°C / Low {day.TempMin}°C");
        }
        return sb.ToString();
    }
}
