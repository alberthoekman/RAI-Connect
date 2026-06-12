using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Rai.Identity.Authorization;

namespace Rai.Tests;

public sealed class PermissionHandlerTests
{
    private static AuthorizationHandlerContext MakeContext(PermissionRequirement req, ClaimsPrincipal user)
    {
        return new AuthorizationHandlerContext([req], user, null);
    }

    [Fact]
    public async Task Handler_succeeds_when_permission_claim_is_present()
    {
        var requirement = new PermissionRequirement("users:read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "users:read")], "Test"));
        var context = MakeContext(requirement, user);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_fails_when_permission_claim_is_absent()
    {
        var requirement = new PermissionRequirement("users:write");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "users:read")], "Test"));
        var context = MakeContext(requirement, user);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_fails_for_unauthenticated_user()
    {
        var requirement = new PermissionRequirement("users:read");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = MakeContext(requirement, user);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
