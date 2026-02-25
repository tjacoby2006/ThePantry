using Microsoft.EntityFrameworkCore;
using ThePantry.Domain;

namespace ThePantry.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<UsageHistory> UsageHistories { get; set; } = null!;
    public DbSet<ScanQueueItem> ScanQueueItems { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100).HasDefaultValue("Pantry");
            entity.Property(e => e.Upc).HasMaxLength(50);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Upc);
        });
        
        modelBuilder.Entity<UsageHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.InventoryItem)
                  .WithMany(i => i.UsageHistories)
                  .HasForeignKey(e => e.InventoryItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasIndex(e => e.Timestamp);
        });
        
        modelBuilder.Entity<ScanQueueItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Upc).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RawData).HasMaxLength(500);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductDescription).HasMaxLength(1000);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Upc);
        });
    }
}