using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Dispatch;

namespace Rai.Tests;

/// <summary>
/// Test server for the Integration Hub using an in-memory SQLite database.
/// Uses <see cref="WebhookDispatcher"/> as the TEntryPoint assembly anchor to avoid
/// ambiguity with the Identity project's Program class in the same test assembly.
/// The dispatcher background service is removed so tests control dispatch timing directly.
/// </summary>
public sealed class HubWebFactory : WebApplicationFactory<WebhookDispatcher>
{
    private readonly SqliteConnection _connection;

    public HubWebFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace production DbContext with in-memory SQLite
            services.RemoveAll<DbContextOptions<HubDbContext>>();
            services.RemoveAll<HubDbContext>();
            services.AddDbContext<HubDbContext>(o => o.UseSqlite(_connection));

            // Remove background dispatcher — tests call DispatchBatchAsync directly
            services.RemoveAll<IHostedService>();
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:InboundHmacSecret"]  = "test-inbound-secret",
                ["Hub:OutboundHmacSecret"] = "test-outbound-secret",
                ["Hub:TicketingWebhookUrl"] = "http://localhost:9999/webhook",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
