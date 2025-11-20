using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantCareApp.Services;

public record WeatherInfo(string LocationLabel, WeatherSnapshot Current, IReadOnlyList<DailyWeather> Daily);

public record WeatherSnapshot(
    double TemperatureC,
    double FeelsLikeC,
    int Humidity,
    string Conditions,
    double WindSpeedKph,
    double PressureHpa,
    double DewPointC,
    double CloudCoverPct,
    double PrecipitationMm,
    double UvIndex,
    double MoonPhase,
    DateTime Sunrise,
    DateTime Sunset,
    DateTime RetrievedAt);

public record DailyWeather(
    DateTime Date,
    double MinTempC,
    double MaxTempC,
    string Conditions,
    double PrecipitationChance,
    double UvIndex);

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

    public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude, string? locationLabel = null, CancellationToken cancellationToken = default)
    {
        WeatherInfo? result = await TryFetchAdvancedAsync(latitude, longitude, locationLabel, cancellationToken);
        if (result is not null)
        {
            return result;
        }

        result = await TryFetchBasicAsync(latitude, longitude, locationLabel, cancellationToken);
        if (result is not null)
        {
            return result;
        }

        throw new InvalidOperationException("No pudimos obtener datos meteorológicos en este momento. Comprueba tu conexión e inténtalo nuevamente.");
    }

    private async Task<WeatherInfo?> TryFetchAdvancedAsync(double latitude, double longitude, string? locationLabel, CancellationToken cancellationToken)
    {
        try
        {
            var invariant = CultureInfo.InvariantCulture;
            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(invariant)}&longitude={longitude.ToString(invariant)}" +
                "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,apparent_temperature,pressure_msl,dew_point_2m,precipitation,cloud_cover" +
                "&daily=temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max,precipitation_probability_max,weather_code,moon_phase" +
                "&timezone=auto";

            await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
            var response = await JsonSerializer.DeserializeAsync<WeatherResponse>(stream, SerializerOptions, cancellationToken);
            if (response?.Current is not { } current)
            {
                return null;
            }

            var (daily, todayDetail) = BuildDaily(response.Daily);
            var code = current.WeatherCode ?? todayDetail?.WeatherCode ?? 0;
            var snapshot = new WeatherSnapshot(
                Math.Round(current.Temperature2m ?? 0, 1),
                Math.Round(current.ApparentTemperature ?? current.Temperature2m ?? 0, 1),
                (int)Math.Round(current.RelativeHumidity2m ?? 0),
                MapWeatherCode(code),
                Math.Round((current.WindSpeed10m ?? 0) * 3.6, 1),
                Math.Round(current.PressureMsl ?? 0, 0),
                Math.Round(current.DewPoint2m ?? 0, 1),
                Math.Round(current.CloudCover ?? 0, 0),
                Math.Round(current.Precipitation ?? 0, 1),
                Math.Round(todayDetail?.UvIndex ?? 0, 1),
                todayDetail?.MoonPhase ?? 0,
                todayDetail?.Sunrise ?? DateTime.MinValue,
                todayDetail?.Sunset ?? DateTime.MinValue,
                ParseDate(current.Time));

            return new WeatherInfo(locationLabel ?? "Ubicación seleccionada", snapshot, daily);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _logger.LogWarning(ex, "Advanced weather call failed for {Lat},{Lon}", latitude, longitude);
            return null;
        }
    }

    private async Task<WeatherInfo?> TryFetchBasicAsync(double latitude, double longitude, string? locationLabel, CancellationToken cancellationToken)
    {
        try
        {
            var invariant = CultureInfo.InvariantCulture;
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(invariant)}&longitude={longitude.ToString(invariant)}&current_weather=true&timezone=auto";
            await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
            var response = await JsonSerializer.DeserializeAsync<BasicWeatherResponse>(stream, SerializerOptions, cancellationToken);
            var current = response?.CurrentWeather;
            if (current is null)
            {
                return null;
            }

            var snapshot = new WeatherSnapshot(
                Math.Round(current.Temperature, 1),
                Math.Round(current.Temperature, 1),
                0,
                MapWeatherCode(current.WeatherCode),
                Math.Round(current.WindSpeed, 1),
                0,
                0,
                0,
                0,
                0,
                0,
                DateTime.MinValue,
                DateTime.MinValue,
                ParseDate(current.Time));

            return new WeatherInfo(locationLabel ?? "Ubicación seleccionada", snapshot, Array.Empty<DailyWeather>());
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _logger.LogWarning(ex, "Basic weather call failed for {Lat},{Lon}", latitude, longitude);
            return null;
        }
    }

    private static bool IsRecoverable(Exception ex) =>
        ex is HttpRequestException or JsonException or InvalidOperationException;

    private static (List<DailyWeather> Daily, DailyDetail? Today) BuildDaily(DailyBlock? daily)
    {
        var output = new List<DailyWeather>();
        DailyDetail? today = null;
        if (daily?.Time is null)
        {
            return (output, today);
        }

        for (var i = 0; i < daily.Time.Length; i++)
        {
            var date = ParseDate(daily.Time[i]);
            var code = (int)GetValue(daily.WeatherCode, i);
            var detail = new DailyDetail(
                date,
                GetValue(daily.TemperatureMin, i),
                GetValue(daily.TemperatureMax, i),
                MapWeatherCode(code),
                GetValue(daily.PrecipitationChance, i),
                GetValue(daily.UvIndexMax, i),
                ParseDate(GetString(daily.Sunrise, i)),
                ParseDate(GetString(daily.Sunset, i)),
                GetValue(daily.MoonPhase, i),
                code);

            output.Add(detail.ToPublic());
            today ??= detail;
        }

        return (output, today);
    }

    private static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : DateTime.UtcNow;
    }

    private static double GetValue(double[]? source, int index)
    {
        if (source is null || index < 0 || index >= source.Length)
        {
            return 0;
        }

        return source[index];
    }

    private static int GetValue(int[]? source, int index)
    {
        if (source is null || index < 0 || index >= source.Length)
        {
            return 0;
        }

        return source[index];
    }

    private static string GetString(string[]? source, int index)
    {
        if (source is null || index < 0 || index >= source.Length)
        {
            return string.Empty;
        }

        return source[index];
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
        [property: JsonPropertyName("current")] CurrentWeather? Current,
        [property: JsonPropertyName("daily")] DailyBlock? Daily);

    private record CurrentWeather(
        [property: JsonPropertyName("time")] string Time,
        [property: JsonPropertyName("temperature_2m")] double? Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] double? RelativeHumidity2m,
        [property: JsonPropertyName("weather_code")] int? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed10m,
        [property: JsonPropertyName("apparent_temperature")] double? ApparentTemperature,
        [property: JsonPropertyName("pressure_msl")] double? PressureMsl,
        [property: JsonPropertyName("dew_point_2m")] double? DewPoint2m,
        [property: JsonPropertyName("precipitation")] double? Precipitation,
        [property: JsonPropertyName("cloud_cover")] double? CloudCover);

    private record DailyBlock(
        [property: JsonPropertyName("time")] string[]? Time,
        [property: JsonPropertyName("temperature_2m_min")] double[]? TemperatureMin,
        [property: JsonPropertyName("temperature_2m_max")] double[]? TemperatureMax,
        [property: JsonPropertyName("sunrise")] string[]? Sunrise,
        [property: JsonPropertyName("sunset")] string[]? Sunset,
        [property: JsonPropertyName("uv_index_max")] double[]? UvIndexMax,
        [property: JsonPropertyName("precipitation_probability_max")] int[]? PrecipitationChance,
        [property: JsonPropertyName("weather_code")] int[]? WeatherCode,
        [property: JsonPropertyName("moon_phase")] double[]? MoonPhase);

    private record DailyDetail(
        DateTime Date,
        double MinTemp,
        double MaxTemp,
        string Conditions,
        double PrecipitationChance,
        double UvIndex,
        DateTime Sunrise,
        DateTime Sunset,
        double MoonPhase,
        int WeatherCode)
    {
        public DailyWeather ToPublic() => new(Date, MinTemp, MaxTemp, Conditions, PrecipitationChance, UvIndex);
    }

    private record BasicWeatherResponse(
        [property: JsonPropertyName("current_weather")] BasicCurrent? CurrentWeather);

    private record BasicCurrent(
        [property: JsonPropertyName("time")] string Time,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("windspeed")] double WindSpeed,
        [property: JsonPropertyName("weathercode")] int WeatherCode);
}
