namespace Rai.Identity.Models;

/// <summary>Join entity between <see cref="ApplicationRole"/> and <see cref="Permission"/>.</summary>
public sealed class RolePermission
{
    /// <summary>Role identifier.</summary>
    public string RoleId { get; set; } = default!;

    /// <summary>Permission identifier.</summary>
    public int PermissionId { get; set; }

    /// <summary>Navigation to role.</summary>
    public ApplicationRole Role { get; set; } = default!;

    /// <summary>Navigation to permission.</summary>
    public Permission Permission { get; set; } = default!;
}
