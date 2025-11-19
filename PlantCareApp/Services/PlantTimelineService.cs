using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using PlantCareApp.Data;
using PlantCareApp.Models;

namespace PlantCareApp.Services;

public class PlantTimelineService
{
    private readonly AppDbContext _db;
    private readonly ImageStorageService _storage;
    private readonly PhotoAnalysisService _photoAnalysis;

    public PlantTimelineService(AppDbContext db, ImageStorageService storage, PhotoAnalysisService photoAnalysis)
    {
        _db = db;
        _storage = storage;
        _photoAnalysis = photoAnalysis;
    }

    public async Task<List<PlantTimelineEvent>> GetTimelineAsync(int plantId)
    {
        return await _db.PlantTimelineEvents
            .AsNoTracking()
            .Include(e => e.Photo)
            .Where(e => e.PlantId == plantId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<int, PlantTimelineEvent>> GetLatestEventsAsync(IEnumerable<int> plantIds)
    {
        var ids = plantIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, PlantTimelineEvent>();
        }

        var events = await _db.PlantTimelineEvents
            .AsNoTracking()
            .Include(e => e.Photo)
            .Where(e => ids.Contains(e.PlantId))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = new Dictionary<int, PlantTimelineEvent>();
        foreach (var evt in events)
        {
            if (!result.ContainsKey(evt.PlantId))
            {
                result[evt.PlantId] = evt;
            }
        }

        return result;
    }

    public async Task<PlantTimelineEvent> LogPlantCreatedAsync(Plant plant, CancellationToken cancellationToken = default)
    {
        var evt = new PlantTimelineEvent
        {
            PlantId = plant.Id,
            Type = PlantTimelineEventType.Created,
            Title = "¡Planta añadida!",
            Description = $"Registrada el {plant.CreatedAt.ToLocalTime():dd MMM yyyy}.",
            Icon = "bi bi-sparkles",
            AccentCss = "pill-success"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task<PlantTimelineEvent> LogWateringAsync(int plantId, DateTime completedAt, string? note = null, CancellationToken cancellationToken = default)
    {
        var plant = await _db.Plants.FindAsync(new object[] { plantId }, cancellationToken);
        var evt = new PlantTimelineEvent
        {
            PlantId = plantId,
            Type = PlantTimelineEventType.Watering,
            Title = plant is null ? "Riego actualizado" : $"Riego de {plant.Name}",
            Description = note ?? $"Último riego registrado el {completedAt.ToLocalTime():dd MMM yyyy}.",
            CreatedAt = completedAt,
            Icon = "bi bi-calendar-event",
            AccentCss = "pill-info"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task<PlantTimelineEvent> AddNoteAsync(int plantId, string note, CancellationToken cancellationToken = default)
    {
        var evt = new PlantTimelineEvent
        {
            PlantId = plantId,
            Type = PlantTimelineEventType.Note,
            Title = "Nota agregada",
            Description = note,
            Icon = "bi bi-pencil-square",
            AccentCss = "pill-neutral"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task<PlantTimelineEvent> AddPhotoAsync(int plantId, IBrowserFile file, string? note = null, CancellationToken cancellationToken = default)
    {
        var path = await _storage.SavePlantImageAsync(file, cancellationToken);

        var photo = new PlantPhoto
        {
            PlantId = plantId,
            FilePath = path,
            TakenAt = DateTime.UtcNow,
            AnalysisSummary = note
        };

        await _photoAnalysis.AnalyzePhotoAsync(photo, cancellationToken);

        _db.PlantPhotos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);

        var evt = new PlantTimelineEvent
        {
            PlantId = plantId,
            Type = PlantTimelineEventType.Photo,
            Title = "Nueva foto",
            Description = string.IsNullOrWhiteSpace(photo.AnalysisSummary) ? "Se agregó una foto al historial." : photo.AnalysisSummary,
            PhotoId = photo.Id,
            Icon = "bi bi-image",
            AccentCss = "pill-warning"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task<PlantTimelineEvent?> LogUpdateAsync(Plant plant, string? summary, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var evt = new PlantTimelineEvent
        {
            PlantId = plant.Id,
            Type = PlantTimelineEventType.Updated,
            Title = "Ficha actualizada",
            Description = summary,
            Icon = "bi bi-arrow-repeat",
            AccentCss = "pill-neutral"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task LogReminderScheduledAsync(int plantId, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        var evt = new PlantTimelineEvent
        {
            PlantId = plantId,
            Type = PlantTimelineEventType.Reminder,
            Title = "Nuevo recordatorio",
            Description = $"Próximo riego programado para el {dueDate:dd MMM yyyy}.",
            Icon = "bi bi-alarm",
            AccentCss = "pill-info"
        };

        _db.PlantTimelineEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePhotoAsync(int photoId, CancellationToken cancellationToken = default)
    {
        var photo = await _db.PlantPhotos.FindAsync(new object[] { photoId }, cancellationToken);
        if (photo is null)
        {
            return;
        }

        _storage.DeleteImage(photo.FilePath);

        var relatedEvents = await _db.PlantTimelineEvents
            .Where(e => e.PhotoId == photoId)
            .ToListAsync(cancellationToken);

        if (relatedEvents.Count > 0)
        {
            _db.PlantTimelineEvents.RemoveRange(relatedEvents);
        }

        _db.PlantPhotos.Remove(photo);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
