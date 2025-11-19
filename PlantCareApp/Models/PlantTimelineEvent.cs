using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class PlantTimelineEvent
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public PlantTimelineEventType Type { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? PhotoId { get; set; }

    public PlantPhoto? Photo { get; set; }

    [StringLength(80)]
    public string? Icon { get; set; }

    [StringLength(40)]
    public string? AccentCss { get; set; }
}
