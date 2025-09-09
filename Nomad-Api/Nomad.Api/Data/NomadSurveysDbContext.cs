using Microsoft.EntityFrameworkCore;
using Nomad.Api.Models;

namespace Nomad.Api.Data;

public class NomadSurveysDbContext : DbContext
{
    public NomadSurveysDbContext(DbContextOptions<NomadSurveysDbContext> options) : base(options)
    {
    }

    // DbSets for entities
    public DbSet<Survey> Surveys { get; set; }

    // Additional DbSets will be added here as we create more entities
    // public DbSet<Question> Questions { get; set; }
    // public DbSet<Response> Responses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Entity configurations
        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Additional entity configurations will be added here
    }
}
