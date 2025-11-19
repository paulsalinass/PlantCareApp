using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantCareApp.Services;

public record LocationSuggestion(string Name, string? Country, double Latitude, double Longitude, string? Timezone);

public class LocationLookupService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationLookupService> _logger;

    public LocationLookupService(HttpClient httpClient, ILogger<LocationLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationSuggestion>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<LocationSuggestion>();
        }

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=5&language=es&format=json";

        try
        {
            await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
            var response = await JsonSerializer.DeserializeAsync<GeoResponse>(stream, SerializerOptions, cancellationToken);
            if (response?.Results is null)
            {
                return Array.Empty<LocationSuggestion>();
            }

            return response.Results
                .Select(r => new LocationSuggestion(r.Name ?? r.Admin1 ?? "Ubicación", r.Country, r.Latitude, r.Longitude, r.Timezone))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location search failed for {Query}", query);
            throw;
        }
    }

    public async Task<LocationSuggestion?> ReverseLookupAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/reverse?latitude={latitude}&longitude={longitude}&count=1&language=es";
        try
        {
            await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
            var response = await JsonSerializer.DeserializeAsync<GeoResponse>(stream, SerializerOptions, cancellationToken);
            var result = response?.Results?.FirstOrDefault();
            if (result is null)
            {
                return null;
            }

            return new LocationSuggestion(result.Name ?? result.Admin1 ?? "Ubicación", result.Country, result.Latitude, result.Longitude, result.Timezone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse lookup failed for {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    private record GeoResponse([property: JsonPropertyName("results")] List<GeoResult>? Results);

    private record GeoResult(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("timezone")] string? Timezone,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("admin1")] string? Admin1);
}
