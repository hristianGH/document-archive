using Archive.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Archive.Data;

public class ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    public DbSet<PartEntity> Parts => Set<PartEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(document =>
        {
            document.Property(d => d.IndexValues).HasColumnType("jsonb");
            document.HasIndex(d => new { d.Cabinet, d.StoredAt, d.Id });
            document.HasMany(d => d.Parts)
                .WithOne()
                .HasForeignKey(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartEntity>(part =>
        {
            part.HasIndex(p => new { p.DocumentId, p.Position }).IsUnique();
            part.HasIndex(p => p.FilePath).IsUnique();
        });
    }
}
