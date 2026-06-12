using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Rai.Identity.Authorization;
using Rai.Identity.Data;
using Rai.Identity.Models;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Rai.Identity.Controllers;

/// <summary>Handles the OIDC authorization, token, logout, and userinfo endpoints.</summary>
public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityDbContext db) : Controller
{
    // ------------------------------------------------------------------
    // Authorization endpoint — redirects to login or auto-approves
    // ------------------------------------------------------------------
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request is null.");

        // If not authenticated, redirect to the login page.
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? [.. Request.Form] : [.. Request.Query]),
                });
        }

        var user = await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("User not found.");

        var identity = await BuildClaimsIdentityAsync(user, request);
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ------------------------------------------------------------------
    // Token endpoint
    // ------------------------------------------------------------------
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request is null.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = await userManager.FindByIdAsync(
            result.Principal!.GetClaim(Claims.Subject)!)
            ?? throw new InvalidOperationException("The user associated with the token no longer exists.");

        var identity = await BuildClaimsIdentityAsync(user, request);
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ------------------------------------------------------------------
    // End session (logout) endpoint
    // ------------------------------------------------------------------
    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EndSession()
    {
        await signInManager.SignOutAsync();
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    // ------------------------------------------------------------------
    // UserInfo endpoint
    // ------------------------------------------------------------------
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> UserInfo()
    {
        var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;
        var user = await userManager.FindByIdAsync(claimsPrincipal.GetClaim(Claims.Subject)!);
        if (user is null) return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id,
            [Claims.Email] = user.Email!,
            [Claims.Name] = user.DisplayName ?? user.Email!,
        };

        // Include roles and permissions if requested
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
            claims["roles"] = roles;

        return Ok(claims);
    }

    // ------------------------------------------------------------------
    // Helper: build a ClaimsIdentity with all relevant claims
    // ------------------------------------------------------------------
    private async Task<ClaimsIdentity> BuildClaimsIdentityAsync(
        ApplicationUser user,
        OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id)
                .SetClaim(Claims.Email, user.Email)
                .SetClaim(Claims.Name, user.DisplayName ?? user.Email!);

        // Add roles
        var roles = await userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, [.. roles]);

        // Add permissions from roles
        var permissionNames = await PermissionClaimsTransformation.GetPermissionsForRolesAsync(db, roles);
        identity.SetClaims("permission", [.. permissionNames]);

        // Determine which scopes were requested and granted
        var scopes = request.GetScopes();
        identity.SetScopes(scopes);
        identity.SetResources(await GetResourcesAsync(scopes));

        // Control which claims go into the access token vs. the id token
        identity.SetDestinations(GetDestinations);

        return identity;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name =>
                claim.Subject!.HasScope(Scopes.Profile)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            Claims.Email =>
                claim.Subject!.HasScope(Scopes.Email)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            Claims.Role =>
                claim.Subject!.HasScope("roles")
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            "permission" =>
                claim.Subject!.HasScope("permissions")
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            _ => [Destinations.AccessToken],
        };
    }

    private static Task<IEnumerable<string>> GetResourcesAsync(IEnumerable<string> scopes) =>
        Task.FromResult<IEnumerable<string>>([]);
}
