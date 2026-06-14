using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Rai.Identity.Api;
using Rai.Identity.Authorization;
using Rai.Identity.Data;
using Rai.Identity.Models;
using Rai.Identity.Seed;
using Rai.Shared.Health;
using Rai.Shared.Logging;
using OidcScopes = OpenIddict.Abstractions.OpenIddictConstants.Scopes;

var builder = WebApplication.CreateBuilder(args);

SerilogBootstrap.Configure(builder, "Identity");

// ------------------------------------------------------------------
// Database — Postgres in Docker, SQLite for local dotnet run
// ------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException("ConnectionStrings:Identity is required. Set it via env var or appsettings.");

builder.Services.AddDbContext<IdentityDbContext>(o =>
    o.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
     .UseOpenIddict()
     .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ------------------------------------------------------------------
// ASP.NET Core Identity
// ------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

// Override the default /Identity/Account/Login path so our Razor page at /Account/Login is used.
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
});

// ------------------------------------------------------------------
// OpenIddict — OIDC/OAuth2 server
// ------------------------------------------------------------------
builder.Services.AddOpenIddict()
    .AddCore(o => o.UseEntityFrameworkCore()
                   .UseDbContext<IdentityDbContext>())
    .AddServer(o =>
    {
        // Fix the issuer so tokens always carry http://localhost:5100 regardless of which
        // hostname the request arrived on (needed when mock services connect via the
        // internal docker-compose hostname "identity" but browsers use "localhost").
        var issuer = builder.Configuration["Identity:Issuer"] ?? "http://localhost:5100";
        o.SetIssuer(new Uri(issuer));

        o.SetAuthorizationEndpointUris("/connect/authorize")
         .SetTokenEndpointUris("/connect/token")
         .SetEndSessionEndpointUris("/connect/endsession")
         .SetUserInfoEndpointUris("/connect/userinfo");

        o.RegisterScopes(
            OidcScopes.OpenId, OidcScopes.Profile, OidcScopes.Email,
            OidcScopes.OfflineAccess, "roles", "permissions");

        o.AllowAuthorizationCodeFlow()
         .RequireProofKeyForCodeExchange();
        o.AllowRefreshTokenFlow();

        // Signing + encryption keys — in production replace with X.509 / Azure Key Vault certs.
        o.AddDevelopmentEncryptionCertificate()
         .AddDevelopmentSigningCertificate();

        o.UseAspNetCore()
         .EnableAuthorizationEndpointPassthrough()
         .EnableTokenEndpointPassthrough()
         .EnableEndSessionEndpointPassthrough()
         .EnableUserInfoEndpointPassthrough()
         .DisableTransportSecurityRequirement(); // Allow HTTP in dev/Docker
    })
    .AddValidation(o =>
    {
        o.UseLocalServer();
        o.UseAspNetCore();
    });

// ------------------------------------------------------------------
// Permission-based RBAC
// ------------------------------------------------------------------
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPermissions.UsersRead,     p => p.AddAuthenticationSchemes("OpenIddict.Validation.AspNetCore").AddRequirements(new PermissionRequirement(AppPermissions.UsersRead)))
    .AddPolicy(AppPermissions.UsersWrite,    p => p.AddAuthenticationSchemes("OpenIddict.Validation.AspNetCore").AddRequirements(new PermissionRequirement(AppPermissions.UsersWrite)))
    .AddPolicy(AppPermissions.RolesManage,   p => p.AddAuthenticationSchemes("OpenIddict.Validation.AspNetCore").AddRequirements(new PermissionRequirement(AppPermissions.RolesManage)))
    .AddPolicy(AppPermissions.ContactsWrite, p => p.AddAuthenticationSchemes("OpenIddict.Validation.AspNetCore").AddRequirements(new PermissionRequirement(AppPermissions.ContactsWrite)));

// ------------------------------------------------------------------
// Misc services
// ------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<DataSeeder>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ------------------------------------------------------------------
// App pipeline
// ------------------------------------------------------------------
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHealth();

// OpenIddict's built-in endpoints are handled as middleware passthrough.
// The connect/* endpoints are registered automatically by UseAspNetCore().

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program { }
