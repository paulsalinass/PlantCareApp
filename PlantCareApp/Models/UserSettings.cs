namespace PlantCareApp.Models;

public class UserSettings
{
    public string DisplayName { get; set; } = "Plant lover";
    public string PlanTier { get; set; } = "Free";
    public bool EnableAiInsights { get; set; } = true;
    public bool EnableCommunity { get; set; } = false;
    public NotificationPreferences Notifications { get; set; } = new();
}

public class NotificationPreferences
{
    public bool WateringAlerts { get; set; } = true;
    public bool FertilizingAlerts { get; set; } = true;
    public bool PruningAlerts { get; set; } = true;
    public bool EmailChannel { get; set; } = true;
    public bool PushChannel { get; set; } = false;
    public string QuietHours { get; set; } = "22:00 - 07:00";
}
