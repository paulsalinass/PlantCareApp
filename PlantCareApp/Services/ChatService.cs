using Microsoft.EntityFrameworkCore;
using PlantCareApp.Data;
using PlantCareApp.Models;

namespace PlantCareApp.Services;

public class ChatService(AppDbContext dbContext)
{
    private readonly AppDbContext _db = dbContext;

    public async Task<PlantConversation> GetOrCreateConversationAsync(int plantId)
    {
        var conversation = await _db.PlantConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.PlantId == plantId);

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new PlantConversation
        {
            PlantId = plantId,
            Title = $"Chat planta #{plantId}"
        };

        _db.PlantConversations.Add(conversation);
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task AddMessageAsync(int conversationId, MessageSender sender, string content, string? attachmentPath = null)
    {
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            Sender = sender,
            Content = content,
            AttachmentPath = attachmentPath,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(int conversationId)
    {
        return await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
