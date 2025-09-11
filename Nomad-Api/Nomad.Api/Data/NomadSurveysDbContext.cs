using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nomad.Api.Entities;

namespace Nomad.Api.Data;

public class NomadSurveysDbContext : IdentityDbContext<ApplicationUser, TenantRole, Guid>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NomadSurveysDbContext(DbContextOptions<NomadSurveysDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Get current tenant ID from HTTP context
    private Guid? CurrentTenantId => _httpContextAccessor.HttpContext?.Items["TenantId"] as Guid?;

    // DbSets for entities
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<UserTenantRole> UserTenantRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Additional DbSets will be added here as we create more entities
    // public DbSet<Question> Questions { get; set; }
    // public DbSet<Response> Responses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentityTables(modelBuilder);
        ConfigureEntityRelationships(modelBuilder);
        ConfigureGlobalQueryFilters(modelBuilder);
    }

    private void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        // Rename Identity tables to avoid conflicts
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<TenantRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }

    private void ConfigureEntityRelationships(ModelBuilder modelBuilder)
    {
        // Tenant configurations
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Company configurations
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Industry).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactPersonName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactPersonEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContactPersonRole).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactPersonPhone).HasMaxLength(20);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Tenant)
                  .WithOne(t => t.Company)
                  .HasForeignKey<Company>(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ContactPerson)
                  .WithMany()
                  .HasForeignKey(e => e.ContactPersonId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ApplicationUser configurations
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Users)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TenantRole configurations
        modelBuilder.Entity<TenantRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.TenantRoles)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UserTenantRole configurations
        modelBuilder.Entity<UserTenantRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserTenantRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserTenantRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Permission configurations
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // RolePermission configurations
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


    }

    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply global query filters for tenant isolation
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Company>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<TenantRole>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId || e.TenantId == null);
        modelBuilder.Entity<UserTenantRole>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId || e.TenantId == null);

        // Add query filter for RolePermission to match TenantRole filter
        modelBuilder.Entity<RolePermission>().HasQueryFilter(rp => CurrentTenantId == null || rp.Role.TenantId == CurrentTenantId || rp.Role.TenantId == null);
    }

    public override int SaveChanges()
    {
        SetTenantId();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var tenantId = CurrentTenantId;
        if (tenantId == null) return;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // Set TenantId for new entities that have this property
                var tenantIdProperty = entry.Entity.GetType().GetProperty("TenantId");
                if (tenantIdProperty != null && tenantIdProperty.PropertyType == typeof(Guid))
                {
                    var currentValue = tenantIdProperty.GetValue(entry.Entity);
                    if (currentValue == null || (Guid)currentValue == Guid.Empty)
                    {
                        tenantIdProperty.SetValue(entry.Entity, tenantId);
                    }
                }
            }
        }
    }
}
