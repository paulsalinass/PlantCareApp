using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantCareApp.Services;

public record WeatherInfo(double TemperatureC, int Humidity, string Conditions, double WindSpeedKph, DateTime RetrievedAt);

public class WeatherService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto";

        await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
        var response = await JsonSerializer.DeserializeAsync<WeatherResponse>(stream, SerializerOptions, cancellationToken);
        var current = response?.Current;
        if (current is null)
        {
            throw new InvalidOperationException("No recibimos datos meteorológicos del proveedor.");
        }

        var conditions = MapWeatherCode(current.WeatherCode ?? 0);
        var retrievedAt = DateTime.TryParse(current.Time, out var parsed) ? parsed : DateTime.UtcNow;

        return new WeatherInfo(
            Math.Round(current.Temperature2m ?? 0, 1),
            (int)Math.Round(current.RelativeHumidity2m ?? 0),
            conditions,
            Math.Round((current.WindSpeed10m ?? 0) * 3.6, 1), // m/s to km/h
            retrievedAt);
    }

    private static string MapWeatherCode(int code) => code switch
    {
        0 => "Despejado",
        1 or 2 => "Parcialmente nublado",
        3 => "Nublado",
        45 or 48 => "Niebla",
        51 or 53 or 55 => "Llovizna",
        61 or 63 or 65 => "Lluvia",
        66 or 67 => "Lluvia helada",
        71 or 73 or 75 => "Nieve",
        80 or 81 or 82 => "Chubascos",
        95 or 96 or 99 => "Tormenta",
        _ => "Condición desconocida"
    };

    private record WeatherResponse(
        [property: JsonPropertyName("current")] CurrentWeather? Current);

    private record CurrentWeather(
        [property: JsonPropertyName("time")] string Time,
        [property: JsonPropertyName("temperature_2m")] double? Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] double? RelativeHumidity2m,
        [property: JsonPropertyName("weather_code")] int? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed10m);
}
