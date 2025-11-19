using System.Text.Json;
using PlantCareApp.Models;

namespace PlantCareApp.Services;

public class UserSettingsService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public UserSettingsService(IHostEnvironment environment)
    {
        var appData = Path.Combine(environment.ContentRootPath, "AppData");
        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "user-settings.json");
    }

    public async Task<UserSettings> GetAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaults = new UserSettings();
                await SaveInternalAsync(defaults);
                return defaults;
            }

            await using var stream = File.OpenRead(_filePath);
            var data = await JsonSerializer.DeserializeAsync<UserSettings>(stream, Options);
            return data ?? new UserSettings();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveAsync(UserSettings settings)
    {
        await _mutex.WaitAsync();
        try
        {
            await SaveInternalAsync(settings);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task SaveInternalAsync(UserSettings settings)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, settings, Options);
    }
}
