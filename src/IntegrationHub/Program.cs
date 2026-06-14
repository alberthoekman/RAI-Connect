using Microsoft.EntityFrameworkCore;
using Serilog;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Dispatch;
using Rai.Shared.Health;
using Rai.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

SerilogBootstrap.Configure(builder, "IntegrationHub");

// ------------------------------------------------------------------
// Database — Postgres in Docker, SQLite for local dotnet run
// ------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Hub");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<HubDbContext>(o =>
        o.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
         .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
}
else
{
    builder.Services.AddDbContext<HubDbContext>(o =>
        o.UseSqlite("Data Source=hub.db"));
}

// ------------------------------------------------------------------
// HTTP client for outbound webhook delivery
// ------------------------------------------------------------------
builder.Services.AddHttpClient("dispatcher", c =>
{
    c.Timeout = TimeSpan.FromSeconds(10);
});

// ------------------------------------------------------------------
// Background dispatcher
// ------------------------------------------------------------------
builder.Services.AddHostedService<WebhookDispatcher>();

// ------------------------------------------------------------------
// Misc
// ------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HubDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();
app.MapControllers();
app.MapHealth();

app.Run();

public partial class Program { }
