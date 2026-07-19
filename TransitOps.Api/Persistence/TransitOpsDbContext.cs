using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Domain;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContext(DbContextOptions<TransitOpsDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();
        user.ToTable("app_users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Username).HasMaxLength(80).IsRequired();
        user.Property(item => item.Email).HasMaxLength(254).IsRequired();
        user.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(item => item.Role).HasConversion<short>();
        user.HasIndex(item => item.Username).IsUnique();
        user.HasIndex(item => item.Email).IsUnique();
    }
}
