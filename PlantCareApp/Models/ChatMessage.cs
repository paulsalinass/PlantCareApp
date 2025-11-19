using System.ComponentModel.DataAnnotations;

namespace PlantCareApp.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public PlantConversation Conversation { get; set; } = null!;

    public MessageSender Sender { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? AttachmentPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
