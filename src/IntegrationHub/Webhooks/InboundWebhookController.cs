using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Models;
using Rai.Shared.Hmac;
using System.Text;

namespace Rai.IntegrationHub.Webhooks;

/// <summary>Receives inbound webhook events, HMAC-verifies them, and enqueues them in the outbox.</summary>
[ApiController, Route("api/webhooks")]
public sealed class InboundWebhookController(
    HubDbContext db,
    IConfiguration config,
    ILogger<InboundWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Receives an event from the CRM service and enqueues it for delivery to the Ticketing service.
    /// Returns 200 immediately; actual delivery is handled asynchronously by <see cref="Dispatch.WebhookDispatcher"/>.
    /// </summary>
    [HttpPost("crm")]
    public async Task<IActionResult> ReceiveCrmEvent()
    {
        // Read raw body for HMAC verification before binding JSON
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var secret = config["Hub:InboundHmacSecret"] ?? "change-in-prod";
        var signature = Request.Headers[HmacSigner.SignatureHeader].FirstOrDefault() ?? string.Empty;

        if (!HmacSigner.Verify(rawBody, secret, signature))
        {
            logger.LogWarning("Rejected inbound webhook — HMAC signature mismatch.");
            return Unauthorized("Invalid HMAC signature.");
        }

        var eventType = Request.Headers["X-Event-Type"].FirstOrDefault() ?? "unknown";
        var eventId = Request.Headers["X-Event-Id"].FirstOrDefault();

        // Idempotency: ignore duplicate event ids
        if (eventId is not null && await db.OutboxMessages.AnyAsync(m => m.Id == Guid.Parse(eventId)))
        {
            logger.LogInformation("Duplicate event {EventId} ignored.", eventId);
            return Ok(new { status = "duplicate" });
        }

        var ticketingUrl = config["Hub:TicketingWebhookUrl"]
                           ?? "http://localhost:5400/api/webhooks/crm-contact-created";

        var message = new OutboxMessage
        {
            Id = eventId is not null ? Guid.Parse(eventId) : Guid.NewGuid(),
            EventType = eventType,
            Payload = rawBody,
            TargetUrl = ticketingUrl,
        };

        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Enqueued event {EventId} ({EventType}) for delivery to {TargetUrl}.",
            message.Id, message.EventType, message.TargetUrl);

        return Ok(new { status = "accepted", eventId = message.Id });
    }

    /// <summary>Returns the current outbox state — useful for demos and debugging.</summary>
    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutbox()
    {
        var messages = await db.OutboxMessages
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new
            {
                m.Id, m.EventType, m.Status,
                m.AttemptCount, m.CreatedAt,
                m.LastAttemptAt, m.LastError, m.NextAttemptAt,
            })
            .ToListAsync();
        return Ok(messages);
    }
}
