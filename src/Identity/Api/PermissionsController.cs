using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rai.Identity.Authorization;
using Rai.Identity.Data;

namespace Rai.Identity.Api;

/// <summary>Returns the full list of defined permissions.</summary>
[ApiController, Route("api/permissions"), Authorize(Policy = AppPermissions.RolesManage)]
public sealed class PermissionsController(IdentityDbContext db) : ControllerBase
{
    /// <summary>Lists all permissions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var perms = await db.Permissions
            .Select(p => new { p.Id, p.Name, p.Description })
            .ToListAsync();
        return Ok(perms);
    }
}
