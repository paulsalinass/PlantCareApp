using PlantCareApp.Models;

namespace PlantCareApp.Services;

public record CareRecommendation(string Title, string Description, string? ProductUrl = null);

public class RecommendationService
{
    // TODO: Integrar motor real basado en especie, clima y datos históricos.
    public IEnumerable<CareRecommendation> GetRecommendations(Plant plant, WeatherInfo? weather)
    {
        var items = new List<CareRecommendation>
        {
            new("Riego", $"Riega cada {plant.WateringFrequencyDays ?? 7} día(s)."),
            new("Revisión", "Observa hojas amarillas o manchas una vez por semana."),
        };

        if (weather is not null && weather.Current.TemperatureC > 28)
        {
            items.Add(new("Protección solar", "Considera mover la planta a sombra parcial durante la tarde."));
        }

        items.Add(new("Producto sugerido", "Fertilizante balanceado cada 2 meses.", "https://example.com/fertilizante"));

        return items;
    }

    public IEnumerable<CareRecommendation> GetTimelineInsights(Plant plant, IEnumerable<PlantTimelineEvent> events)
    {
        var list = new List<CareRecommendation>();
        var ordered = events.OrderByDescending(e => e.CreatedAt).ToList();
        var latestWater = ordered.FirstOrDefault(e => e.Type == PlantTimelineEventType.Watering);
        if (latestWater is not null && (DateTime.UtcNow - latestWater.CreatedAt).TotalDays > (plant.WateringFrequencyDays ?? 7) + 1)
        {
            list.Add(new CareRecommendation("Revisa la humedad", "Ya pasó más del intervalo de riego habitual. Verifica si la tierra sigue húmeda."));
        }

        var latestPhoto = ordered.FirstOrDefault(e => e.Type == PlantTimelineEventType.Photo && e.Photo?.HealthScore is not null);
        if (latestPhoto?.Photo?.HealthScore is double score && score < 0.6)
        {
            list.Add(new CareRecommendation("Analiza la foto reciente", "La última foto detectó posibles signos de estrés. Observa hojas y tallos para descartar plagas."));
        }

        if (!list.Any() && latestWater is not null)
        {
            list.Add(new CareRecommendation("Todo en orden", $"El último riego se registró el {latestWater.CreatedAt.ToLocalTime():dd MMM}. Continúa con el plan actual."));
        }

        return list;
    }
}
