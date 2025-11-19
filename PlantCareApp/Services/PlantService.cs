using Microsoft.EntityFrameworkCore;
using PlantCareApp.Data;
using PlantCareApp.Models;
using System.Linq;

namespace PlantCareApp.Services;

public class PlantService(AppDbContext dbContext, PlantTimelineService timelineService)
{
    private readonly AppDbContext _db = dbContext;
    private readonly PlantTimelineService _timeline = timelineService;

    public async Task<List<Plant>> GetPlantsAsync(string? ownerId = null)
    {
        var query = _db.Plants
            .AsNoTracking()
            .Include(p => p.Reminders)
            .AsQueryable();

        if (!string.IsNullOrEmpty(ownerId))
        {
            query = query.Where(p => p.OwnerId == ownerId);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Plant?> GetPlantAsync(int id)
    {
        return await _db.Plants
            .Include(p => p.Photos)
            .Include(p => p.Reminders)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddPlantAsync(Plant plant)
    {
        plant.CreatedAt = DateTime.UtcNow;
        plant.UpdatedAt = DateTime.UtcNow;
        plant.NextWateringDate = CalculateNextWateringDate(plant.LastWateredAt, plant.WateringFrequencyDays);

        _db.Plants.Add(plant);
        await _db.SaveChangesAsync();

        await EnsureOrUpdateReminderAsync(plant);
        await _timeline.LogPlantCreatedAsync(plant);
    }

    public async Task UpdatePlantAsync(Plant plant)
    {
        var original = await _db.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plant.Id);
        if (original is null)
        {
            return;
        }

        plant.UpdatedAt = DateTime.UtcNow;
        plant.NextWateringDate = CalculateNextWateringDate(plant.LastWateredAt, plant.WateringFrequencyDays);

        var changeSummary = BuildChangeSummary(original, plant);

        _db.Plants.Update(plant);
        await _db.SaveChangesAsync();

        await EnsureOrUpdateReminderAsync(plant);

        if (!string.IsNullOrWhiteSpace(changeSummary))
        {
            await _timeline.LogUpdateAsync(plant, changeSummary);
        }
    }

    public async Task DeletePlantAsync(int id)
    {
        var plant = await _db.Plants.FindAsync(id);
        if (plant is null) return;

        _db.Plants.Remove(plant);
        await _db.SaveChangesAsync();
    }

    public async Task<List<PlantReminder>> GetDueRemindersAsync(DateTime referenceDate)
    {
        return await _db.PlantReminders
            .Include(r => r.Plant)
            .Where(r => r.DueDate.Date <= referenceDate.Date && r.CompletedAt == null)
            .OrderBy(r => r.DueDate)
            .ToListAsync();
    }

    public async Task CompleteReminderAsync(int reminderId)
    {
        var reminder = await _db.PlantReminders.FindAsync(reminderId);
        if (reminder is null) return;

        var completedAt = DateTime.UtcNow;
        reminder.CompletedAt = completedAt;

        Plant? plantToUpdate = null;
        if (reminder.Type == ReminderType.Watering)
        {
            plantToUpdate = await _db.Plants.FindAsync(reminder.PlantId);
            if (plantToUpdate is not null)
            {
                plantToUpdate.LastWateredAt = completedAt;
                plantToUpdate.NextWateringDate =
                    CalculateNextWateringDate(plantToUpdate.LastWateredAt, plantToUpdate.WateringFrequencyDays);
                _db.Plants.Update(plantToUpdate);
            }
        }

        await _db.SaveChangesAsync();

        if (plantToUpdate is not null)
        {
            await EnsureOrUpdateReminderAsync(plantToUpdate);
            await _timeline.LogWateringAsync(plantToUpdate.Id, completedAt);
        }
    }

    private async Task EnsureOrUpdateReminderAsync(Plant plant)
    {
        var openReminder = await _db.PlantReminders.FirstOrDefaultAsync(r =>
            r.PlantId == plant.Id &&
            r.Type == ReminderType.Watering &&
            r.CompletedAt == null);

        if (!plant.WateringFrequencyDays.HasValue)
        {
            if (openReminder is not null)
            {
                _db.PlantReminders.Remove(openReminder);
                await _db.SaveChangesAsync();
            }

            return;
        }

        var dueDate = (plant.NextWateringDate ??
                       CalculateNextWateringDate(plant.LastWateredAt, plant.WateringFrequencyDays) ??
                       DateTime.UtcNow.AddDays(plant.WateringFrequencyDays.Value)).Date;

        var createdReminder = false;
        if (openReminder is null)
        {
            _db.PlantReminders.Add(new PlantReminder
            {
                PlantId = plant.Id,
                Type = ReminderType.Watering,
                DueDate = dueDate
            });
            createdReminder = true;
        }
        else
        {
            openReminder.DueDate = dueDate;
            _db.PlantReminders.Update(openReminder);
        }

        await _db.SaveChangesAsync();

        if (createdReminder)
        {
            await _timeline.LogReminderScheduledAsync(plant.Id, dueDate);
        }
    }

    private static string BuildChangeSummary(Plant previous, Plant updated)
    {
        var changes = new List<string>();
        AppendChange(changes, "Zona", previous.LocationArea, updated.LocationArea);
        AppendChange(changes, "Ubicación", CombineLocation(previous.LocationName, previous.Country), CombineLocation(updated.LocationName, updated.Country));
        AppendChange(changes, "Frecuencia de riego", previous.WateringFrequencyDays, updated.WateringFrequencyDays, "día(s)");
        AppendChange(changes, "Horas de sol", previous.EstimatedSunHours, updated.EstimatedSunHours, "h");
        if (previous.IsIndoors != updated.IsIndoors)
        {
            changes.Add($"Ambiente: {(updated.IsIndoors ? "Interior" : "Exterior")}");
        }
        if (!string.Equals(previous.Notes, updated.Notes, StringComparison.Ordinal))
        {
            changes.Add("Notas actualizadas");
        }

        return string.Join(" · ", changes);
    }

    private static void AppendChange(List<string> changes, string label, string? oldValue, string? newValue)
    {
        if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
        {
            changes.Add($"{label}: {(string.IsNullOrWhiteSpace(newValue) ? "Sin dato" : newValue)}");
        }
    }

    private static void AppendChange(List<string> changes, string label, int? oldValue, int? newValue, string suffix = "")
    {
        if (oldValue != newValue)
        {
            changes.Add($"{label}: {(newValue.HasValue ? $"{newValue}{suffix}" : "Sin dato")}");
        }
    }

    private static string CombineLocation(string? name, string? country)
    {
        var segments = new[] { name, country }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim());
        return string.Join(", ", segments);
    }

    private static DateTime? CalculateNextWateringDate(DateTime? lastWateredAt, int? frequencyDays)
    {
        if (!frequencyDays.HasValue)
        {
            return null;
        }

        var reference = (lastWateredAt ?? DateTime.UtcNow).Date;
        return reference.AddDays(frequencyDays.Value);
    }
}
