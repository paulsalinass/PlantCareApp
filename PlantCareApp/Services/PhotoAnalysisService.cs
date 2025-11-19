using PlantCareApp.Models;

namespace PlantCareApp.Services;

public class PhotoAnalysisService
{
    // TODO: Integrar modelo real de visión.
    public Task<PlantPhoto> AnalyzePhotoAsync(PlantPhoto photo, CancellationToken cancellationToken = default)
    {
        photo.AnalysisSummary = "Análisis pendiente de IA.";
        photo.HealthScore = 0.8;
        return Task.FromResult(photo);
    }
}
