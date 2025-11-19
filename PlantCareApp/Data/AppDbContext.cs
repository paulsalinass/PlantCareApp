using Microsoft.EntityFrameworkCore;
using PlantCareApp.Models;

namespace PlantCareApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<PlantPhoto> PlantPhotos => Set<PlantPhoto>();
    public DbSet<PlantReminder> PlantReminders => Set<PlantReminder>();
    public DbSet<PlantConversation> PlantConversations => Set<PlantConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<PlantTimelineEvent> PlantTimelineEvents => Set<PlantTimelineEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Plant>()
            .HasMany(p => p.Photos)
            .WithOne(p => p.Plant)
            .HasForeignKey(p => p.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plant>()
            .HasMany(p => p.Reminders)
            .WithOne(r => r.Plant)
            .HasForeignKey(r => r.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plant>()
            .HasMany(p => p.Conversations)
            .WithOne(c => c.Plant)
            .HasForeignKey(c => c.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlantConversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Plant>()
            .HasMany(p => p.TimelineEvents)
            .WithOne(t => t.Plant)
            .HasForeignKey(t => t.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlantTimelineEvent>()
            .HasOne(t => t.Photo)
            .WithMany()
            .HasForeignKey(t => t.PhotoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
