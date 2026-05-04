namespace Modules.Ai.Infrastructure.Weather;

/// <summary>
/// Clean DTOs for weather data. Used by both the WeatherSkill (AI) and the
/// Weather endpoint (Web.Api). Defined in the AI infrastructure layer since
/// the service lives here — the Web.Api endpoint references these via the
/// project reference it already has.
/// </summary>

public sealed record CurrentWeatherDto(
    string City,
    string Country,
    double Temperature,
    int Humidity,
    double WindSpeed,
    string Condition,
    int WeatherCode);

public sealed record ForecastDayDto(
    string Date,
    double TempMax,
    double TempMin,
    string Condition,
    int WeatherCode);

public sealed record WeatherResponseDto(
    CurrentWeatherDto Current,
    IReadOnlyList<ForecastDayDto> Forecast);
