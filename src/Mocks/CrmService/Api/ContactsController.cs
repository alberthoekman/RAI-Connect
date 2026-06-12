using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rai.Mocks.CrmService.Models;
using Rai.Mocks.CrmService.Services;
using Rai.Shared.Hmac;
using System.Text;
using System.Text.Json;

namespace Rai.Mocks.CrmService.Api;

/// <summary>Manages CRM contacts. Creating a contact fires a webhook to the Integration Hub.</summary>
[ApiController, Route("api/contacts")]
public sealed class ContactsController(
    ContactStore store,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<ContactsController> logger) : ControllerBase
{
    /// <summary>Returns all contacts.</summary>
    [HttpGet, Authorize]
    public IActionResult GetAll() => Ok(store.GetAll());

    /// <summary>Creates a contact and fires a webhook event to the Integration Hub.</summary>
    [HttpPost, Authorize]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest req)
    {
        var contact = new Contact
        {
            Name = req.Name,
            Email = req.Email,
            CreatedBy = User.Identity?.Name,
        };
        store.Add(contact);

        logger.LogInformation("Created contact {ContactId} ({Name})", contact.Id, contact.Name);

        await SendWebhookAsync(contact);

        return CreatedAtAction(nameof(GetAll), new { id = contact.Id },
            new { contact.Id, contact.Name, contact.Email, contact.CreatedAt });
    }

    internal async Task SendWebhookAsync(Contact contact)
    {
        var secret = config["Crm:HmacSecret"] ?? "change-in-prod";
        var hubUrl = config["Crm:HubWebhookUrl"] ?? "http://localhost:5200/api/webhooks/crm";
        var eventId = Guid.NewGuid();

        var payload = JsonSerializer.Serialize(new
        {
            contactId = contact.Id,
            name = contact.Name,
            email = contact.Email,
            createdAt = contact.CreatedAt,
        });

        var signature = HmacSigner.Sign(payload, secret);
        var client = httpFactory.CreateClient("hub");

        using var request = new HttpRequestMessage(HttpMethod.Post, hubUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(HmacSigner.SignatureHeader, signature);
        request.Headers.Add("X-Event-Type", "crm.contact.created");
        request.Headers.Add("X-Event-Id", eventId.ToString());

        try
        {
            using var response = await client.SendAsync(request);
            logger.LogInformation(
                "Sent webhook for contact {ContactId} to hub — HTTP {StatusCode}",
                contact.Id, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send webhook for contact {ContactId}", contact.Id);
        }
    }
}

public sealed record CreateContactRequest(string Name, string? Email);
