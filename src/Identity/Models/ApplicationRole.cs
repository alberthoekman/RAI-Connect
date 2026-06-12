using Microsoft.AspNetCore.Identity;

namespace Rai.Identity.Models;

/// <summary>Application role used in RBAC.</summary>
public sealed class ApplicationRole : IdentityRole
{
    /// <summary>Permissions granted to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
