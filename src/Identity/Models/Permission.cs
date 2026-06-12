using System.ComponentModel.DataAnnotations;

namespace Rai.Identity.Models;

/// <summary>A named permission that can be granted to roles and emitted as a claim.</summary>
public sealed class Permission
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Machine-readable name, e.g. <c>users:write</c>.</summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    /// <summary>Human-readable description.</summary>
    [MaxLength(250)]
    public string? Description { get; set; }

    /// <summary>Roles that have this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
