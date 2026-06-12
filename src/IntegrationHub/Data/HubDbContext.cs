using Microsoft.EntityFrameworkCore;
using Rai.IntegrationHub.Models;

namespace Rai.IntegrationHub.Data;

/// <summary>EF Core context for the Integration Hub — outbox messages only.</summary>
public sealed class HubDbContext(DbContextOptions<HubDbContext> options) : DbContext(options)
{
    /// <summary>Outbox messages awaiting delivery (or already delivered / dead-lettered).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<OutboxMessage>(e =>
        {
            e.HasIndex(m => m.Status);
            e.HasIndex(m => m.NextAttemptAt);
        });
    }
}
