using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Formatting.Compact;

namespace Rai.Shared.Logging;

/// <summary>Shared Serilog bootstrap used by every service in the solution.</summary>
public static class SerilogBootstrap
{
    /// <summary>
    /// Configures Serilog for structured JSON console output.
    /// Reads overrides from configuration (e.g. <c>Serilog:MinimumLevel</c>).
    /// </summary>
    public static void Configure(WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((ctx, cfg) =>
        {
            cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console(new CompactJsonFormatter());
        });
    }
}
