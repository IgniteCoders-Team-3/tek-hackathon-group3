namespace Web.Api.Infrastructure.Weather;

/// <summary>
/// Clean DTOs returned by the Weather endpoint and consumed by the WeatherSkill.
/// Mirrors the Open-Meteo JSON shape after mapping — callers never see the raw API response.
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
