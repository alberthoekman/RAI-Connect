using Microsoft.AspNetCore.Mvc;
using Rai.Mocks.TicketingService.Models;
using Rai.Mocks.TicketingService.Services;
using Rai.Shared.Hmac;
using System.Text;
using System.Text.Json;

namespace Rai.Mocks.TicketingService.Api;

/// <summary>Consumes webhook events from the Integration Hub. Handlers are idempotent.</summary>
[ApiController, Route("api/webhooks")]
public sealed class WebhookController(
    TicketStore store,
    IConfiguration config,
    ILogger<WebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Receives a <c>crm.contact.created</c> event and creates a ticket.
    /// Deduplicates on <c>X-Event-Id</c> to handle retries safely.
    /// </summary>
    [HttpPost("crm-contact-created")]
    public async Task<IActionResult> ContactCreated()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();

        var secret = config["Ticketing:HmacSecret"] ?? "change-in-prod";
        var signature = Request.Headers[HmacSigner.SignatureHeader].FirstOrDefault() ?? string.Empty;

        if (!HmacSigner.Verify(rawBody, secret, signature))
        {
            logger.LogWarning("Rejected inbound webhook — HMAC mismatch.");
            return Unauthorized("Invalid HMAC signature.");
        }

        var eventIdHeader = Request.Headers["X-Event-Id"].FirstOrDefault();
        if (!Guid.TryParse(eventIdHeader, out var eventId))
            eventId = Guid.NewGuid();

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var contactName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown";

        var ticket = new Ticket
        {
            SourceEventId = eventId,
            Title = $"New contact: {contactName}",
            Description = rawBody,
        };

        if (!store.TryAdd(ticket))
        {
            logger.LogInformation("Duplicate event {EventId} ignored.", eventId);
            return Ok(new { status = "duplicate" });
        }

        logger.LogInformation(
            "Created ticket {TicketId} from event {EventId} (contact: {ContactName}).",
            ticket.Id, eventId, contactName);

        return Ok(new { status = "created", ticketId = ticket.Id });
    }

    /// <summary>Returns all tickets — for demo verification.</summary>
    [HttpGet("~/api/tickets")]
    public IActionResult GetTickets() => Ok(store.GetAll());
}
