using Microsoft.EntityFrameworkCore;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Models;
using Rai.Shared.Hmac;

namespace Rai.IntegrationHub.Dispatch;

/// <summary>
/// Background service that continuously polls the outbox and dispatches pending messages
/// to their target URLs with exponential-backoff retries up to <see cref="MaxAttempts"/>.
/// Exhausted messages are moved to the dead-letter state.
/// </summary>
public sealed class WebhookDispatcher(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<WebhookDispatcher> logger) : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebhookDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in WebhookDispatcher loop.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HubDbContext>();

        // Split: filter by status in SQL, then by NextAttemptAt in memory.
        // DateTimeOffset comparisons don't translate on the SQLite provider used in tests;
        // pending messages are few in practice so the extra in-memory step is negligible.
        var now = DateTimeOffset.UtcNow;
        var candidates = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .ToListAsync(ct);

        var messages = candidates
            .Where(m => m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .Take(10)
            .ToList();

        foreach (var msg in messages)
        {
            await DeliverAsync(db, msg, ct);
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private async Task DeliverAsync(HubDbContext db, OutboxMessage msg, CancellationToken ct)
    {
        var secret = config["Hub:OutboundHmacSecret"] ?? "change-in-prod";
        var signature = HmacSigner.Sign(msg.Payload, secret);

        var client = httpClientFactory.CreateClient("dispatcher");
        using var request = new HttpRequestMessage(HttpMethod.Post, msg.TargetUrl)
        {
            Content = new StringContent(msg.Payload, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(HmacSigner.SignatureHeader, signature);
        request.Headers.Add("X-Event-Type", msg.EventType);
        request.Headers.Add("X-Event-Id", msg.Id.ToString());

        msg.AttemptCount++;
        msg.LastAttemptAt = DateTimeOffset.UtcNow;

        try
        {
            using var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                msg.Status = OutboxMessageStatus.Delivered;
                logger.LogInformation(
                    "Delivered event {EventId} ({EventType}) to {TargetUrl} on attempt {Attempt}.",
                    msg.Id, msg.EventType, msg.TargetUrl, msg.AttemptCount);
                return;
            }

            msg.LastError = $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            msg.LastError = ex.Message;
        }

        if (msg.AttemptCount >= MaxAttempts)
        {
            msg.Status = OutboxMessageStatus.DeadLettered;
            logger.LogWarning(
                "Event {EventId} ({EventType}) moved to dead-letter after {Attempts} attempts. Last error: {Error}",
                msg.Id, msg.EventType, msg.AttemptCount, msg.LastError);
        }
        else
        {
            // Exponential backoff: 5s, 25s, 125s, 625s
            var delay = TimeSpan.FromSeconds(Math.Pow(5, msg.AttemptCount));
            msg.NextAttemptAt = DateTimeOffset.UtcNow.Add(delay);
            logger.LogWarning(
                "Delivery of event {EventId} failed (attempt {Attempt}/{Max}). Retrying at {NextAttempt}. Error: {Error}",
                msg.Id, msg.AttemptCount, MaxAttempts, msg.NextAttemptAt, msg.LastError);
        }
    }
}
