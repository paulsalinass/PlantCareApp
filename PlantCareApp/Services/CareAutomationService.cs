using PlantCareApp.Models;

namespace PlantCareApp.Services;

public record CareAutomationInsight(string Title, string Description, string Severity, string Icon);

public class CareAutomationService
{
    private readonly WeatherService _weatherService;

    public CareAutomationService(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public Task<IReadOnlyList<CareAutomationInsight>> BuildInsightsAsync(Plant plant, WeatherInfo? weather, IEnumerable<PlantTimelineEvent> timeline, CancellationToken cancellationToken = default)
    {
        var insights = new List<CareAutomationInsight>();

        if (plant.LastWateredAt.HasValue && plant.WateringFrequencyDays.HasValue)
        {
            var sinceWater = (DateTime.UtcNow - plant.LastWateredAt.Value).TotalDays;
            if (sinceWater > plant.WateringFrequencyDays.Value + 1)
            {
                insights.Add(new CareAutomationInsight("Riego atrasado", $"Han pasado {sinceWater:F0} días desde el último riego. Programa uno hoy.", "high", "bi bi-exclamation-triangle"));
            }
            else if (sinceWater > plant.WateringFrequencyDays.Value - 1)
            {
                insights.Add(new CareAutomationInsight("Próximo riego", "La planta está por cumplir su frecuencia habitual. Confirma si el sustrato sigue húmedo.", "medium", "bi bi-droplet-half"));
            }
        }

        var latestPhoto = timeline.FirstOrDefault(e => e.Type == PlantTimelineEventType.Photo && e.Photo?.HealthScore is not null);
        if (latestPhoto?.Photo?.HealthScore is double score)
        {
            if (score < 0.5)
            {
                insights.Add(new CareAutomationInsight("Posible estrés", "La última foto sugiere signos de estrés. Revisa plagas y niveles de luz.", "high", "bi bi-emoji-frown"));
            }
            else if (score < 0.75)
            {
                insights.Add(new CareAutomationInsight("Vigila la evolución", "La planta muestra pequeños signos de estrés. Observa hojas jóvenes y riega de forma moderada.", "medium", "bi bi-activity"));
            }
        }

        if (weather is not null)
        {
            if (weather.TemperatureC > 30)
            {
                insights.Add(new CareAutomationInsight("Calor intenso", "El clima actual es muy caluroso. Considera mover la planta a sombra parcial y vaporizar ligeramente.", "high", "bi bi-thermometer-high"));
            }
            else if (weather.Humidity < 30 && plant.IsIndoors)
            {
                insights.Add(new CareAutomationInsight("Humedad baja", "El ambiente está seco. Usa bandejas con agua o humidificador.", "medium", "bi bi-moisture"));
            }
        }

        if (!insights.Any())
        {
            insights.Add(new CareAutomationInsight("Todo bajo control", "No detectamos acciones urgentes. Mantén el plan actual y registra los cuidados.", "low", "bi bi-check2-circle"));
        }

        return Task.FromResult<IReadOnlyList<CareAutomationInsight>>(insights);
    }
}
