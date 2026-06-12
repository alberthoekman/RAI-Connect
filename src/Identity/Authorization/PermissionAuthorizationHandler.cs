using Microsoft.AspNetCore.Authorization;

namespace Rai.Identity.Authorization;

/// <summary>Evaluates <see cref="PermissionRequirement"/> against the <c>permission</c> claims in the token.</summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
