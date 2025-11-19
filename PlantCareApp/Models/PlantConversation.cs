using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class PlantConversation
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    [StringLength(160)]
    public string Title { get; set; } = "Chat IA";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
