using Microsoft.EntityFrameworkCore;
using Register.Domain.Entities;

namespace Register.Infrastructure.Persistence;

public sealed class RegisterDbContext(DbContextOptions<RegisterDbContext> options) : DbContext(options)
{
    public DbSet<RegisteredUser> RegisteredUsers => Set<RegisteredUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegisteredUser>(entity =>
        {
            entity.ToTable("RegisteredUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
        });
    }
}
