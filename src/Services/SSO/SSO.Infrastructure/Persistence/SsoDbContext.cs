using SSO.Domain.Entities;
using SSO.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SSO.Infrastructure.Persistence;

public sealed class SsoDbContext(DbContextOptions<SsoDbContext> options)
    : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LoginAudit>(entity =>
        {
            entity.ToTable("LoginAudits");
            entity.HasKey(x => new { x.UserId, x.OccurredAtUtc });
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
        });
    }
}
