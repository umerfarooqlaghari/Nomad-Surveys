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

    // Participant entities
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Evaluator> Evaluators { get; set; }
    public DbSet<SubjectEvaluator> SubjectEvaluators { get; set; }

    // Employee entity
    public DbSet<Employee> Employees { get; set; }

    // Additional DbSets will be added here as we create more entities
    // public DbSet<Question> Questions { get; set; }
    // public DbSet<Response> Responses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentityTables(modelBuilder);
        ConfigureEntityRelationships(modelBuilder);
        ConfigureParticipantRelationships(modelBuilder);
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
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Grade).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Metadata1).HasMaxLength(255);
            entity.Property(e => e.Metadata2).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Users)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Employee)
                  .WithMany(emp => emp.Users)
                  .HasForeignKey(e => e.EmployeeId)
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

    private void ConfigureParticipantRelationships(ModelBuilder modelBuilder)
    {
        // Subject configurations
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint: one employee can only be a subject once per tenant
            entity.HasIndex(e => new { e.EmployeeId, e.TenantId }).IsUnique();

            // Foreign key to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to Employee
            entity.HasOne(e => e.Employee)
                .WithOne(emp => emp.Subject)
                .HasForeignKey<Subject>(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to ApplicationUser (optional)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Evaluator configurations
        modelBuilder.Entity<Evaluator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint: one employee can only be an evaluator once per tenant
            entity.HasIndex(e => new { e.EmployeeId, e.TenantId }).IsUnique();

            // Foreign key to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key to Employee
            entity.HasOne(e => e.Employee)
                .WithOne(emp => emp.Evaluator)
                .HasForeignKey<Evaluator>(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to ApplicationUser (optional)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SubjectEvaluator configurations (junction table)
        modelBuilder.Entity<SubjectEvaluator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Relationship).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint to prevent duplicate assignments
            entity.HasIndex(e => new { e.SubjectId, e.EvaluatorId }).IsUnique();

            // Foreign key to Subject
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.SubjectEvaluators)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Evaluator
            entity.HasOne(e => e.Evaluator)
                .WithMany(ev => ev.SubjectEvaluators)
                .HasForeignKey(e => e.EvaluatorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Employee configurations
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Number).HasMaxLength(20);
            entity.Property(e => e.EmployeeId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CompanyName).HasMaxLength(100);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Grade).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.ManagerId).HasMaxLength(50);
            entity.Property(e => e.MoreInfo).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraints within tenant
            entity.HasIndex(e => new { e.Email, e.TenantId }).IsUnique();
            entity.HasIndex(e => new { e.EmployeeId, e.TenantId }).IsUnique();

            // Foreign key to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
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

        // Participant query filters for tenant isolation
        modelBuilder.Entity<Subject>().HasQueryFilter(s => CurrentTenantId == null || s.TenantId == CurrentTenantId);
        modelBuilder.Entity<Evaluator>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<SubjectEvaluator>().HasQueryFilter(se => CurrentTenantId == null || se.TenantId == CurrentTenantId);

        // Employee query filter for tenant isolation
        modelBuilder.Entity<Employee>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
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
