using Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rai.Shared.Health;
using Rai.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

SerilogBootstrap.Configure(builder, "CrmService");

// ------------------------------------------------------------------
// OIDC authentication — confidential client against the Identity IdP
// ------------------------------------------------------------------
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(o =>
    {
        o.Cookie.Name = "crm.session";
        o.LoginPath = "/login";
    })
    .AddOpenIdConnect(o =>
    {
        o.Authority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:5100";
        o.ClientId = "crm-service";
        o.ClientSecret = builder.Configuration["Oidc:CrmClientSecret"] ?? "crm-secret-change-in-prod";
        o.ResponseType = OpenIdConnectResponseType.Code;
        o.SaveTokens = true;
        o.GetClaimsFromUserInfoEndpoint = true;
        o.RequireHttpsMetadata = false; // HTTP in dev/Docker
        o.Scope.Clear();
        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");
        o.Scope.Add("offline_access");
        o.Scope.Add("roles");
        o.Scope.Add("permissions");
    });

// ------------------------------------------------------------------
// Outbound HTTP client for hub webhook
// ------------------------------------------------------------------
builder.Services.AddHttpClient("hub", c => c.Timeout = TimeSpan.FromSeconds(5));

// ------------------------------------------------------------------
// Misc
// ------------------------------------------------------------------
builder.Services.AddSingleton<Rai.Mocks.CrmService.Services.ContactStore>();
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHealth();

// Minimal redirect for /login — triggers OIDC challenge
app.MapGet("/login", () => Results.Challenge(
    properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/",
    },
    authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();
