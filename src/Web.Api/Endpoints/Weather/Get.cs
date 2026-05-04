using Microsoft.AspNetCore.Authorization;
using Modules.Ai.Infrastructure.Weather;

namespace Web.Api.Endpoints.Weather;

/// <summary>
/// GET /api/weather?city=Lagos — returns current weather + 5-day forecast from Open-Meteo.
/// No API key required. The city parameter defaults to "Lagos" if omitted.
/// </summary>
public sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("weather", [Authorize] async (
            string? city,
            WeatherService weather,
            CancellationToken ct) =>
        {
            string targetCity = string.IsNullOrWhiteSpace(city) ? "Lagos" : city.Trim();

            WeatherResponseDto? result = await weather.GetWeatherAsync(targetCity, forecastDays: 5, ct);

            return result is null
                ? Results.NotFound(new { error = $"Could not find weather data for '{targetCity}'." })
                : Results.Ok(result);
        })
        .WithTags(Tags.Weather);
    }
}
