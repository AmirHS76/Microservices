using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Persistence
{
    public sealed class ChatReadDbContext(DbContextOptions<ChatReadDbContext> options) : DbContext(options)
    {
        public DbSet<ChatUser> Users => Set<ChatUser>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ChatMessage> Messages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatUser>(builder =>
            {
                builder.HasKey(x => x.UserId);
                builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            });

            modelBuilder.Entity<Conversation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasIndex(x => new { x.FirstUserId, x.SecondUserId }).IsUnique();
                builder.Property(x => x.CreatedAtUtc).IsRequired();
                builder.Property(x => x.LastMessageAtUtc).IsRequired();
            });

            modelBuilder.Entity<ChatMessage>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
                builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
                builder.Property(x => x.CreatedAtUtc).IsRequired();
                builder.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });
                builder.HasIndex(x => new { x.RecipientId, x.Status });
                builder.HasOne(x => x.Conversation)
                    .WithMany(x => x.Messages)
                    .HasForeignKey(x => x.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

}
