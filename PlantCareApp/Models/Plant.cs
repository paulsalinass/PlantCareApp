using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class Plant
{
    public int Id { get; set; }

    [StringLength(64)]
    public string? OwnerId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Species { get; set; } = string.Empty;

    [StringLength(120)]
    public string? LocationName { get; set; }

    [StringLength(120)]
    public string? Country { get; set; }

    [StringLength(80)]
    public string? LocationArea { get; set; }

    public bool IsIndoors { get; set; }

    [Range(0, 24)]
    public int? EstimatedSunHours { get; set; }

    [Range(1, 90)]
    public int? WateringFrequencyDays { get; set; } = 7;

    public DateTime? LastWateredAt { get; set; }

    public DateTime? NextWateringDate { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [StringLength(256)]
    public string? MainPhotoPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlantPhoto> Photos { get; set; } = new List<PlantPhoto>();

    public ICollection<PlantReminder> Reminders { get; set; } = new List<PlantReminder>();

    public ICollection<PlantConversation> Conversations { get; set; } = new List<PlantConversation>();

    public ICollection<PlantTimelineEvent> TimelineEvents { get; set; } = new List<PlantTimelineEvent>();
}
