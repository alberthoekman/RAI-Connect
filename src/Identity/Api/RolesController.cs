using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rai.Identity.Authorization;
using Rai.Identity.Data;
using Rai.Identity.Models;

namespace Rai.Identity.Api;

/// <summary>Manages roles and role-permission assignments.</summary>
[ApiController, Route("api/roles"), Authorize(Policy = AppPermissions.RolesManage)]
public sealed class RolesController(
    RoleManager<ApplicationRole> roleManager,
    IdentityDbContext db) : ControllerBase
{
    /// <summary>Returns all roles with their permissions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await db.Roles
            .OfType<ApplicationRole>()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Select(r => new
            {
                r.Id,
                r.Name,
                Permissions = r.RolePermissions.Select(rp => new { rp.Permission.Id, rp.Permission.Name }),
            })
            .ToListAsync();
        return Ok(roles);
    }

    /// <summary>Creates a new role.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        var result = await roleManager.CreateAsync(new ApplicationRole { Name = req.Name });
        if (!result.Succeeded) return BadRequest(result.Errors);
        return Ok(new { Name = req.Name });
    }

    /// <summary>Grants a permission to a role.</summary>
    [HttpPost("{roleId}/permissions/{permissionId:int}")]
    public async Task<IActionResult> GrantPermission(string roleId, int permissionId)
    {
        var role = await db.Roles.OfType<ApplicationRole>().FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null) return NotFound("Role not found");

        var perm = await db.Permissions.FindAsync(permissionId);
        if (perm is null) return NotFound("Permission not found");

        if (await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            return Conflict("Permission already granted");

        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Revokes a permission from a role.</summary>
    [HttpDelete("{roleId}/permissions/{permissionId:int}")]
    public async Task<IActionResult> RevokePermission(string roleId, int permissionId)
    {
        var rp = await db.RolePermissions.FindAsync(roleId, permissionId);
        if (rp is null) return NotFound();
        db.RolePermissions.Remove(rp);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public sealed record CreateRoleRequest(string Name);
