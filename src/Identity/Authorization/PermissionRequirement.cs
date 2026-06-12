using Microsoft.AspNetCore.Authorization;

namespace Rai.Identity.Authorization;

/// <summary>Authorization requirement that demands a specific permission claim.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>The required permission, e.g. <c>users:write</c>.</summary>
    public string Permission { get; } = permission;
}
