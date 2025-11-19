using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class PlantPhoto
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    [Required, StringLength(256)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    [StringLength(512)]
    public string? AnalysisSummary { get; set; }

    public double? HealthScore { get; set; }
}
