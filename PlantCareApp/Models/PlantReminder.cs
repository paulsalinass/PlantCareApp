using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class PlantReminder
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public ReminderType Type { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(256)]
    public string? Notes { get; set; }
}
