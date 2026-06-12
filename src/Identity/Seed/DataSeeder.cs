using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Rai.Identity.Authorization;
using Rai.Identity.Data;
using Rai.Identity.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Rai.Identity.Seed;

/// <summary>Seeds the database with the initial admin user, roles, permissions, and OIDC clients on startup.</summary>
public sealed class DataSeeder(
    IServiceProvider services,
    ILogger<DataSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolesAsync(scope.ServiceProvider, db, cancellationToken);
        await SeedAdminUserAsync(scope.ServiceProvider, cancellationToken);
        await SeedOidcClientsAsync(scope.ServiceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ------------------------------------------------------------------ permissions

    private async Task SeedPermissionsAsync(IdentityDbContext db, CancellationToken ct)
    {
        foreach (var (name, description) in AppPermissions.All)
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == name, ct))
            {
                db.Permissions.Add(new Permission { Name = name, Description = description });
                logger.LogInformation("Seeded permission {Permission}", name);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ roles

    private async Task SeedRolesAsync(IServiceProvider sp, IdentityDbContext db, CancellationToken ct)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var role = new ApplicationRole { Name = "Admin" };
            await roleManager.CreateAsync(role);
            logger.LogInformation("Seeded role Admin");
        }

        // Grant all permissions to Admin
        var adminRole = await db.Roles
            .Include(r => ((ApplicationRole)r).RolePermissions)
            .OfType<ApplicationRole>()
            .FirstAsync(r => r.Name == "Admin", ct);

        var allPermissions = await db.Permissions.ToListAsync(ct);
        foreach (var perm in allPermissions)
        {
            if (!adminRole.RolePermissions.Any(rp => rp.PermissionId == perm.Id))
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = perm.Id,
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------ admin user

    private async Task SeedAdminUserAsync(IServiceProvider sp, CancellationToken ct)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "admin@rai.local";

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Admin User",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, "Admin1234!");
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, "Admin");
        logger.LogInformation("Seeded admin user {Email}", email);
    }

    // ------------------------------------------------------------------ OIDC clients

    private async Task SeedOidcClientsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var manager = sp.GetRequiredService<IOpenIddictApplicationManager>();

        await EnsureClientAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "admin-spa",
            ClientType = ClientTypes.Public,
            DisplayName = "Admin SPA",
            RedirectUris = { new Uri("http://localhost:5173/callback") },
            PostLogoutRedirectUris = { new Uri("http://localhost:5173") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,  // covers offline_access
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                Permissions.Scopes.Roles,
                $"{Permissions.Prefixes.Scope}permissions",
            },
        }, ct);

        var crmSecret = sp.GetRequiredService<IConfiguration>()["Oidc:CrmClientSecret"]
                        ?? "crm-secret-change-in-prod";

        await EnsureClientAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "crm-service",
            ClientSecret = crmSecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "CRM Service",
            RedirectUris = { new Uri("http://localhost:5300/signin-oidc") },
            PostLogoutRedirectUris = { new Uri("http://localhost:5300/signout-callback-oidc") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                Permissions.Scopes.Roles,
                $"{Permissions.Prefixes.Scope}permissions",
            },
        }, ct);

        var ticketingSecret = sp.GetRequiredService<IConfiguration>()["Oidc:TicketingClientSecret"]
                              ?? "ticketing-secret-change-in-prod";

        await EnsureClientAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "ticketing-service",
            ClientSecret = ticketingSecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "Ticketing Service",
            RedirectUris = { new Uri("http://localhost:5400/signin-oidc") },
            PostLogoutRedirectUris = { new Uri("http://localhost:5400/signout-callback-oidc") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
            },
        }, ct);
    }

    private async Task EnsureClientAsync(
        IOpenIddictApplicationManager manager,
        OpenIddictApplicationDescriptor descriptor,
        CancellationToken ct)
    {
        if (await manager.FindByClientIdAsync(descriptor.ClientId!, ct) is not null)
            return;

        await manager.CreateAsync(descriptor, ct);
        logger.LogInformation("Seeded OIDC client {ClientId}", descriptor.ClientId);
    }
}
