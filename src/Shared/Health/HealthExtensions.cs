using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rai.Shared.Health;

/// <summary>Extension methods for wiring the standard liveness health endpoint.</summary>
public static class HealthExtensions
{
    /// <summary>Maps <c>/health</c> as a liveness probe that returns 200 OK when the service is up.</summary>
    public static WebApplication MapHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503,
            },
        });
        return app;
    }
}
