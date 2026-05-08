using Microsoft.EntityFrameworkCore;
using TokenGuard.Core.Entities;

namespace TokenGuard.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.KeyHash).IsUnique();
            e.Property(x => x.DailyLimitUsd).HasColumnType("TEXT");
            e.HasMany(x => x.UsageRecords)
             .WithOne(x => x.ApiKey)
             .HasForeignKey(x => x.ApiKeyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UsageRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EstimatedCostUsd).HasColumnType("TEXT");
            e.HasIndex(x => new { x.ApiKeyId, x.Timestamp });
        });
    }
}
