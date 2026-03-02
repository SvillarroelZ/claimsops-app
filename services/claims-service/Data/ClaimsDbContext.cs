using Microsoft.EntityFrameworkCore;
using ClaimsService.Models;

namespace ClaimsService.Data;

/// <summary>
/// Entity Framework Core database context for Claims.
/// Maps entities to PostgreSQL tables and manages database operations.
/// </summary>
public class ClaimsDbContext : DbContext
{
    // Purpose: Define EF Core mapping between Claim model and PostgreSQL schema.
    // Input: DbContextOptions configuration and Claim entities.
    // Output: Database context operations and model configuration metadata.
    // Why this exists: Central place to configure tables, constraints, and column behavior.
    /// <summary>
    /// Constructor for dependency injection of DbContextOptions.
    /// </summary>
    /// <param name="options">Configuration options for the database context</param>
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// DbSet representing the Claims table in the database.
    /// </summary>
    public DbSet<Claim> Claims { get; set; }

    /// <summary>
    /// Configure model properties and relationships.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Purpose: Configure entity-to-table rules for Claim persistence.
        // Input: ModelBuilder provided by EF Core.
        // Output: Mapping rules (keys, precision, defaults, constraints).
        // Why this exists: Ensure DB schema matches domain requirements consistently.
        base.OnModelCreating(modelBuilder);

        // Configure Claims table
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MemberId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
        });
    }
}
