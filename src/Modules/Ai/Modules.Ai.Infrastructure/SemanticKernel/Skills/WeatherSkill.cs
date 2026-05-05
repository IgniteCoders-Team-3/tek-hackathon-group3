using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Modules.Ai.Application.Mcp.Clients;
using Modules.Ai.Application.Mcp.Contracts;

namespace Modules.Ai.Infrastructure.SemanticKernel.Skills;

/// <summary>
/// SK skill that calls the Weather MCP plugin so the LLM can get live
/// Open-Meteo weather data through the same MCP transport layer used by
/// all other TelcoPilot skills (alerts, energy, network, osm).
/// </summary>
public sealed class WeatherSkill(IMcpInvoker mcp)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    [KernelFunction("get_current_weather")]
    [Description("Get current weather conditions for a city: temperature, humidity, wind speed, and conditions. " +
                 "Use when the user asks about current weather or when correlating weather with network issues.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("City name, e.g. 'Lagos', 'Ikeja', 'Lekki', 'Abuja'")] string city,
        CancellationToken cancellationToken = default)
    {
        McpInvocationResult result = await mcp.InvokeAsync(
            new McpInvocationRequest("weather", "get_current_weather",
                new Dictionary<string, object?> { ["city"] = city }),
            cancellationToken);

        if (!result.IsSuccess || result.Output is null)
            return $"Could not retrieve weather data for '{city}': {result.Error}";

        // Deserialise the structured output from WeatherMcpPlugin
        string json = result.Output is string s ? s : JsonSerializer.Serialize(result.Output);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        return new StringBuilder()
            .AppendLine($"Weather for {root.GetPropString("city")}, {root.GetPropString("country")}:")
            .AppendLine($"  Condition:   {root.GetPropString("condition")}")
            .AppendLine($"  Temperature: {root.GetPropDouble("temperature")}°C")
            .AppendLine($"  Humidity:    {root.GetPropInt("humidity")}%")
            .AppendLine($"  Wind Speed:  {root.GetPropDouble("windSpeed")} km/h")
            .ToString();
    }

    [KernelFunction("get_weather_forecast")]
    [Description("Get a 5-day weather forecast for a city including daily high/low temperatures and conditions. " +
                 "Use when the user asks about upcoming weather or planning maintenance windows.")]
    public async Task<string> GetWeatherForecastAsync(
        [Description("City name, e.g. 'Lagos', 'Ikeja', 'Lekki', 'Abuja'")] string city,
        CancellationToken cancellationToken = default)
    {
        McpInvocationResult result = await mcp.InvokeAsync(
            new McpInvocationRequest("weather", "get_weather_forecast",
                new Dictionary<string, object?> { ["city"] = city }),
            cancellationToken);

        if (!result.IsSuccess || result.Output is null)
            return $"Could not retrieve forecast for '{city}': {result.Error}";

        string json = result.Output is string s ? s : JsonSerializer.Serialize(result.Output);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        var sb = new StringBuilder();
        sb.AppendLine($"5-day forecast for {root.GetPropString("city")}:");
        if (root.TryGetProperty("days", out JsonElement days))
        {
            foreach (JsonElement day in days.EnumerateArray())
            {
                sb.AppendLine($"  {day.GetPropString("date")}: {day.GetPropString("condition")}, " +
                              $"High {day.GetPropDouble("tempMax")}°C / Low {day.GetPropDouble("tempMin")}°C");
            }
        }
        return sb.ToString();
    }
}

/// <summary>Helper extensions to keep JsonElement reads concise.</summary>
file static class JsonElementExtensions
{
    public static string  GetPropString(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) ? p.GetString() ?? "" : "";
    public static double  GetPropDouble(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.TryGetDouble(out double d) ? d : 0;
    public static int     GetPropInt(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.TryGetInt32(out int i) ? i : 0;
}
