using Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rai.Mocks.TicketingService.Services;
using Rai.Shared.Health;
using Rai.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

SerilogBootstrap.Configure(builder, "TicketingService");

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
        o.Cookie.Name = "ticketing.session";
        o.LoginPath = "/login";
    })
    .AddOpenIdConnect(o =>
    {
        o.Authority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:5100";
        o.ClientId = "ticketing-service";
        o.ClientSecret = builder.Configuration["Oidc:TicketingClientSecret"] ?? "ticketing-secret-change-in-prod";
        o.ResponseType = OpenIdConnectResponseType.Code;
        o.SaveTokens = true;
        o.GetClaimsFromUserInfoEndpoint = true;
        o.RequireHttpsMetadata = false;
        o.Scope.Clear();
        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");
        o.Scope.Add("offline_access");
    });

// ------------------------------------------------------------------
// Misc
// ------------------------------------------------------------------
builder.Services.AddSingleton<TicketStore>();
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

app.MapGet("/login", () => Results.Challenge(
    properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/",
    },
    authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();
