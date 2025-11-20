using System.Linq;
using System.Text.Json;
using PlantCareApp.Models;

namespace PlantCareApp.Services;

public class HomeZoneService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly HomeZone[] _defaults =
    [
        new HomeZone { Name = "Sala", AreaType = "Interior" },
        new HomeZone { Name = "Comedor", AreaType = "Interior" },
        new HomeZone { Name = "Cocina", AreaType = "Interior" },
        new HomeZone { Name = "Dormitorio", AreaType = "Interior" },
        new HomeZone { Name = "Estudio", AreaType = "Interior" },
        new HomeZone { Name = "Patio", AreaType = "Exterior" },
        new HomeZone { Name = "Balcón", AreaType = "Exterior" },
        new HomeZone { Name = "Terraza", AreaType = "Exterior" },
        new HomeZone { Name = "Jardín", AreaType = "Exterior" },
        new HomeZone { Name = "Otro", AreaType = "Mixto" }
    ];

    public HomeZoneService(IHostEnvironment env)
    {
        var appData = Path.Combine(env.ContentRootPath, "AppData");
        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "home-zones.json");
        if (!File.Exists(_filePath))
        {
            var seeded = _defaults.Select(d => new HomeZone
            {
                Id = Guid.NewGuid(),
                Name = d.Name,
                AreaType = d.AreaType,
                Notes = d.Notes,
                CreatedAt = DateTime.UtcNow
            }).ToList();
            var payload = JsonSerializer.Serialize(seeded, SerializerOptions);
            File.WriteAllText(_filePath, payload);
        }
    }

    public async Task<List<HomeZone>> GetZonesAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            var zones = await ReadAsync();
            return zones.OrderBy(z => z.Name).ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<HomeZone> AddZoneAsync(HomeZone zone)
    {
        await _mutex.WaitAsync();
        try
        {
            var zones = await ReadAsync();
            var existing = zones.FirstOrDefault(z => string.Equals(z.Name, zone.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return existing;
            }

            zone.Id = Guid.NewGuid();
            zone.CreatedAt = DateTime.UtcNow;
            zones.Add(zone);
            await WriteAsync(zones);
            return zone;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task UpdateZoneAsync(HomeZone zone)
    {
        await _mutex.WaitAsync();
        try
        {
            var zones = await ReadAsync();
            var existing = zones.FirstOrDefault(z => z.Id == zone.Id);
            if (existing is null) return;

            existing.Name = zone.Name;
            existing.AreaType = zone.AreaType;
            existing.Notes = zone.Notes;
            await WriteAsync(zones);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteZoneAsync(Guid id)
    {
        await _mutex.WaitAsync();
        try
        {
            var zones = await ReadAsync();
            zones.RemoveAll(z => z.Id == id);
            await WriteAsync(zones);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<HomeZone>> ReadAsync()
    {
        await using var stream = File.OpenRead(_filePath);
        var zones = await JsonSerializer.DeserializeAsync<List<HomeZone>>(stream, SerializerOptions);
        return zones ?? new List<HomeZone>();
    }

    private async Task WriteAsync(List<HomeZone> zones)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, zones, SerializerOptions);
    }
}
