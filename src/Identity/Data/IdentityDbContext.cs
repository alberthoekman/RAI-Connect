using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rai.Identity.Models;

namespace Rai.Identity.Data;

/// <summary>EF Core context combining ASP.NET Core Identity tables with OpenIddict and RBAC entities.</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    /// <summary>Permissions that can be assigned to roles.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Join between roles and permissions.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Permission>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Id).UseIdentityByDefaultColumn();

            e.HasIndex(p => p.Name).IsUnique();
        });

        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            e.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
