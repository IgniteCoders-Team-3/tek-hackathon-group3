using Modules.Ai.Application.Mcp.Contracts;
using Modules.Ai.Infrastructure.Weather;

namespace Modules.Ai.Infrastructure.Mcp.Plugins;

/// <summary>
/// MCP plugin that exposes Open-Meteo weather data as callable capabilities.
/// Registered alongside the other internal plugins (alerts, energy, network, osm)
/// and surfaces automatically in the /api/mcp/plugins discovery endpoint.
/// </summary>
internal sealed class WeatherMcpPlugin(WeatherService weather) : IMcpPlugin
{
    public string PluginId   => "weather";
    public string DisplayName => "Weather";
    public McpPluginKind Kind => McpPluginKind.Internal;

    public IReadOnlyList<McpCapability> Capabilities { get; } =
    [
        new McpCapability(
            "get_current_weather",
            "Get current weather conditions (temperature, humidity, wind speed, conditions) for any city. " +
            "Use when the user asks about current weather or when correlating weather with tower/network issues.",
            [
                new McpCapabilityParameter("city", "string",
                    "City name, e.g. 'Lagos', 'Ikeja', 'Abuja', 'Port Harcourt'", IsRequired: true),
            ]),

        new McpCapability(
            "get_weather_forecast",
            "Get a 5-day weather forecast for any city including daily high/low temperatures and conditions. " +
            "Use when the user asks about upcoming weather or planning maintenance around weather windows.",
            [
                new McpCapabilityParameter("city", "string",
                    "City name, e.g. 'Lagos', 'Ikeja', 'Abuja', 'Port Harcourt'", IsRequired: true),
            ]),
    ];

    public async Task<McpInvocationResult> InvokeAsync(
        McpInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string city = request.Arguments.TryGetValue("city", out object? v) && v is string s && !string.IsNullOrWhiteSpace(s)
            ? s.Trim()
            : "Lagos";

        return request.Capability.ToLowerInvariant() switch
        {
            "get_current_weather" => await GetCurrentAsync(request, city, cancellationToken),
            "get_weather_forecast" => await GetForecastAsync(request, city, cancellationToken),
            _ => new McpInvocationResult(request.PluginId, request.Capability, false, null,
                $"Unknown capability '{request.Capability}'.", 0, request.CorrelationId),
        };
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task<McpInvocationResult> GetCurrentAsync(
        McpInvocationRequest request, string city, CancellationToken ct)
    {
        CurrentWeatherDto? result = await weather.GetCurrentWeatherAsync(city, ct);
        if (result is null)
        {
            return Fail(request, $"Could not retrieve weather data for '{city}'. City may not be recognised.");
        }

        return Ok(request, new
        {
            city        = result.City,
            country     = result.Country,
            temperature = result.Temperature,
            humidity    = result.Humidity,
            windSpeed   = result.WindSpeed,
            condition   = result.Condition,
            weatherCode = result.WeatherCode,
        });
    }

    private async Task<McpInvocationResult> GetForecastAsync(
        McpInvocationRequest request, string city, CancellationToken ct)
    {
        IReadOnlyList<ForecastDayDto> forecast = await weather.GetForecastAsync(city, days: 5, ct);
        if (forecast.Count == 0)
        {
            return Fail(request, $"Could not retrieve forecast for '{city}'.");
        }

        return Ok(request, new
        {
            city,
            days = forecast.Select(d => new
            {
                date      = d.Date,
                tempMax   = d.TempMax,
                tempMin   = d.TempMin,
                condition = d.Condition,
            }).ToArray(),
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static McpInvocationResult Ok(McpInvocationRequest req, object output) =>
        new(req.PluginId, req.Capability, IsSuccess: true, Output: output, Error: null, DurationMs: 0, CorrelationId: req.CorrelationId);

    private static McpInvocationResult Fail(McpInvocationRequest req, string error) =>
        new(req.PluginId, req.Capability, IsSuccess: false, Output: null, Error: error, DurationMs: 0, CorrelationId: req.CorrelationId);
}
