using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(StewardshipDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => await db.Database.CanConnectAsync(ct) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database unavailable.");
}
