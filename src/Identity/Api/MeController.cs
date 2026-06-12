using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rai.Identity.Authorization;
using Rai.Identity.Models;

namespace Rai.Identity.Api;

/// <summary>Returns information about the currently authenticated user.</summary>
[ApiController, Route("api/me"), Authorize(AuthenticationSchemes = "OpenIddict.Validation.AspNetCore")]
public sealed class MeController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>Returns the current user's profile and permissions.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var permissions = User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            Roles = roles,
            Permissions = permissions,
        });
    }

    /// <summary>
    /// A protected endpoint that requires the <c>users:read</c> permission.
    /// Used by the demo to confirm that granting/revoking a permission is enforced in real time.
    /// </summary>
    [HttpGet("protected"), Authorize(Policy = AppPermissions.UsersRead)]
    public IActionResult Protected() =>
        Ok(new { Message = "Access granted — you have the users:read permission." });
}
