using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Modules.Ai.Infrastructure.Weather;

/// <summary>
/// Calls the Open-Meteo geocoding and forecast APIs (no API key required).
/// Registered as a typed HttpClient via <c>services.AddHttpClient&lt;WeatherService&gt;()</c>.
/// Lives in the AI infrastructure layer so the WeatherSkill can consume it without
/// a circular project reference — Web.Api already references Modules.Ai.Infrastructure.
/// </summary>
public sealed class WeatherService(HttpClient http, ILogger<WeatherService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Get current weather conditions for the given city.
    /// </summary>
    public async Task<CurrentWeatherDto?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
    {
        GeoResult? geo = await GeocodeAsync(city, ct);
        if (geo is null) return null;

        string url = $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={geo.Latitude}&longitude={geo.Longitude}" +
            $"&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code" +
            $"&timezone=auto";

        try
        {
            using JsonDocument doc = await http.GetFromJsonAsync<JsonDocument>(url, JsonOpts, ct)
                ?? throw new InvalidOperationException("Null response from Open-Meteo forecast API.");

            JsonElement current = doc.RootElement.GetProperty("current");

            int weatherCode = current.GetProperty("weather_code").GetInt32();
            return new CurrentWeatherDto(
                City: geo.Name,
                Country: geo.Country,
                Temperature: current.GetProperty("temperature_2m").GetDouble(),
                Humidity: current.GetProperty("relative_humidity_2m").GetInt32(),
                WindSpeed: current.GetProperty("wind_speed_10m").GetDouble(),
                Condition: WeatherCodeMapper.ToCondition(weatherCode),
                WeatherCode: weatherCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Open-Meteo current weather fetch failed for city '{City}'.", city);
            return null;
        }
    }

    /// <summary>
    /// Get a multi-day weather forecast for the given city.
    /// </summary>
    public async Task<IReadOnlyList<ForecastDayDto>> GetForecastAsync(string city, int days = 5, CancellationToken ct = default)
    {
        GeoResult? geo = await GeocodeAsync(city, ct);
        if (geo is null) return [];

        string url = $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={geo.Latitude}&longitude={geo.Longitude}" +
            $"&daily=temperature_2m_max,temperature_2m_min,weather_code" +
            $"&timezone=auto&forecast_days={days}";

        try
        {
            using JsonDocument doc = await http.GetFromJsonAsync<JsonDocument>(url, JsonOpts, ct)
                ?? throw new InvalidOperationException("Null response from Open-Meteo forecast API.");

            JsonElement daily = doc.RootElement.GetProperty("daily");
            JsonElement dates = daily.GetProperty("time");
            JsonElement maxTemps = daily.GetProperty("temperature_2m_max");
            JsonElement minTemps = daily.GetProperty("temperature_2m_min");
            JsonElement codes = daily.GetProperty("weather_code");

            var forecast = new List<ForecastDayDto>();
            for (int i = 0; i < dates.GetArrayLength(); i++)
            {
                int code = codes[i].GetInt32();
                forecast.Add(new ForecastDayDto(
                    Date: dates[i].GetString()!,
                    TempMax: maxTemps[i].GetDouble(),
                    TempMin: minTemps[i].GetDouble(),
                    Condition: WeatherCodeMapper.ToCondition(code),
                    WeatherCode: code));
            }
            return forecast;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Open-Meteo forecast fetch failed for city '{City}'.", city);
            return [];
        }
    }

    /// <summary>
    /// Get both current weather and forecast in a single call. Geocodes once.
    /// </summary>
    public async Task<WeatherResponseDto?> GetWeatherAsync(string city, int forecastDays = 5, CancellationToken ct = default)
    {
        GeoResult? geo = await GeocodeAsync(city, ct);
        if (geo is null) return null;

        string url = $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={geo.Latitude}&longitude={geo.Longitude}" +
            $"&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code" +
            $"&daily=temperature_2m_max,temperature_2m_min,weather_code" +
            $"&timezone=auto&forecast_days={forecastDays}";

        try
        {
            using JsonDocument doc = await http.GetFromJsonAsync<JsonDocument>(url, JsonOpts, ct)
                ?? throw new InvalidOperationException("Null response from Open-Meteo API.");

            // Parse current
            JsonElement current = doc.RootElement.GetProperty("current");
            int currentCode = current.GetProperty("weather_code").GetInt32();
            var currentDto = new CurrentWeatherDto(
                City: geo.Name,
                Country: geo.Country,
                Temperature: current.GetProperty("temperature_2m").GetDouble(),
                Humidity: current.GetProperty("relative_humidity_2m").GetInt32(),
                WindSpeed: current.GetProperty("wind_speed_10m").GetDouble(),
                Condition: WeatherCodeMapper.ToCondition(currentCode),
                WeatherCode: currentCode);

            // Parse daily forecast
            JsonElement daily = doc.RootElement.GetProperty("daily");
            JsonElement dates = daily.GetProperty("time");
            JsonElement maxTemps = daily.GetProperty("temperature_2m_max");
            JsonElement minTemps = daily.GetProperty("temperature_2m_min");
            JsonElement codes = daily.GetProperty("weather_code");

            var forecast = new List<ForecastDayDto>();
            for (int i = 0; i < dates.GetArrayLength(); i++)
            {
                int code = codes[i].GetInt32();
                forecast.Add(new ForecastDayDto(
                    Date: dates[i].GetString()!,
                    TempMax: maxTemps[i].GetDouble(),
                    TempMin: minTemps[i].GetDouble(),
                    Condition: WeatherCodeMapper.ToCondition(code),
                    WeatherCode: code));
            }

            return new WeatherResponseDto(currentDto, forecast);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Open-Meteo combined weather fetch failed for city '{City}'.", city);
            return null;
        }
    }

    // ── Geocoding ──────────────────────────────────────────────────────────

    private async Task<GeoResult?> GeocodeAsync(string city, CancellationToken ct)
    {
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en";
        try
        {
            using JsonDocument doc = await http.GetFromJsonAsync<JsonDocument>(url, JsonOpts, ct)
                ?? throw new InvalidOperationException("Null response from Open-Meteo geocoding API.");

            if (!doc.RootElement.TryGetProperty("results", out JsonElement results) ||
                results.GetArrayLength() == 0)
            {
                logger.LogInformation("Geocoding returned no results for city '{City}'.", city);
                return null;
            }

            JsonElement first = results[0];
            return new GeoResult(
                Name: first.GetProperty("name").GetString()!,
                Country: first.TryGetProperty("country", out JsonElement c) ? c.GetString() ?? "" : "",
                Latitude: first.GetProperty("latitude").GetDouble(),
                Longitude: first.GetProperty("longitude").GetDouble());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Open-Meteo geocoding failed for city '{City}'.", city);
            return null;
        }
    }

    private sealed record GeoResult(string Name, string Country, double Latitude, double Longitude);
}
