using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rai.Identity.Data;
using Rai.Identity.Models;

namespace Rai.Identity.Authorization;

/// <summary>
/// Enriches the claims principal with <c>permission</c> claims derived from the user's roles.
/// Runs after cookie or token authentication so the admin API sees correct permissions.
/// </summary>
public sealed class PermissionClaimsTransformation(
    UserManager<ApplicationUser> userManager,
    IdentityDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only transform authenticated principals that don't already have permission claims.
        if (!principal.Identity?.IsAuthenticated ?? true)
            return principal;

        if (principal.HasClaim(c => c.Type == "permission"))
            return principal;

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return principal;

        var roleNames = await userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
            return principal;

        var permissions = await GetPermissionsForRolesAsync(db, roleNames);
        if (permissions.Count == 0)
            return principal;

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;
        foreach (var perm in permissions)
            identity.AddClaim(new Claim("permission", perm));

        return clone;
    }

    /// <summary>Returns distinct permission names for the given role names.</summary>
    public static async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IdentityDbContext db, IEnumerable<string> roleNames)
    {
        return await db.RolePermissions
            .Where(rp => roleNames.Contains(rp.Role.Name!))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }
}
