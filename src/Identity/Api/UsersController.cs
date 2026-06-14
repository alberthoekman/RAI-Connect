using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rai.Identity.Authorization;
using AppPerms = Rai.Identity.Authorization.AppPermissions;
using Rai.Identity.Models;

namespace Rai.Identity.Api;

/// <summary>Manages users: list, create, update, delete, role assignment.</summary>
[ApiController, Route("api/users")]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : ControllerBase
{
    /// <summary>Returns all users with their assigned roles.</summary>
    [HttpGet, Authorize(Policy = AppPerms.UsersRead)]
    public async Task<IActionResult> GetAll()
    {
        var users = userManager.Users.ToList();
        var result = new List<object>(users.Count);
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.UserName, u.DisplayName, Roles = roles });
        }
        return Ok(result);
    }

    /// <summary>Returns a single user by id.</summary>
    [HttpGet("{id}"), Authorize(Policy = AppPerms.UsersRead)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new { user.Id, user.Email, user.UserName, user.DisplayName, Roles = roles });
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost, Authorize(Policy = AppPerms.UsersWrite)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName,
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new { user.Id, user.Email, user.DisplayName });
    }

    /// <summary>Updates display name of a user.</summary>
    [HttpPut("{id}"), Authorize(Policy = AppPerms.UsersWrite)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.DisplayName = req.DisplayName;
        await userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>Deletes a user.</summary>
    [HttpDelete("{id}"), Authorize(Policy = AppPerms.UsersWrite)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        await userManager.DeleteAsync(user);
        return NoContent();
    }

    /// <summary>Assigns a role to a user.</summary>
    [HttpPost("{id}/roles"), Authorize(Policy = AppPerms.RolesManage)]
    public async Task<IActionResult> AssignRole(string id, [FromBody] RoleRequest req)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found");
        if (!await roleManager.RoleExistsAsync(req.RoleName))
            return BadRequest("Role does not exist");

        var result = await userManager.AddToRoleAsync(user, req.RoleName);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return NoContent();
    }

    /// <summary>Removes a role from a user.</summary>
    [HttpDelete("{id}/roles/{roleName}"), Authorize(Policy = AppPerms.RolesManage)]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found");
        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded) return BadRequest(result.Errors);
        return NoContent();
    }
}

public sealed record CreateUserRequest(string Email, string DisplayName, string Password);
public sealed record UpdateUserRequest(string DisplayName);
public sealed record RoleRequest(string RoleName);
