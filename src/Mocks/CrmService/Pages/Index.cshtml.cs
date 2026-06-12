using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rai.Mocks.CrmService.Models;
using Rai.Mocks.CrmService.Services;
using Rai.Shared.Hmac;
using System.Text;
using System.Text.Json;

namespace Rai.Mocks.CrmService.Pages;

/// <summary>Protected home page — requires OIDC login (SSO from IdP session).</summary>
[Authorize]
public sealed class IndexModel(
    ContactStore store,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<IndexModel> logger) : PageModel
{
    public string UserName { get; private set; } = string.Empty;
    public IReadOnlyList<Contact> Contacts { get; private set; } = [];
    public string? LastCreated { get; private set; }

    public void OnGet()
    {
        UserName = User.FindFirst("name")?.Value
                   ?? User.Identity?.Name
                   ?? "Unknown";
        Contacts = store.GetAll();
    }

    public async Task<IActionResult> OnPostAsync(string name, string? email)
    {
        UserName = User.FindFirst("name")?.Value
                   ?? User.Identity?.Name
                   ?? "Unknown";

        var contact = new Contact
        {
            Name = name,
            Email = email,
            CreatedBy = UserName,
        };
        store.Add(contact);
        Contacts = store.GetAll();
        LastCreated = name;

        await SendWebhookAsync(contact);
        return Page();
    }

    private async Task SendWebhookAsync(Contact contact)
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

        using var req = new HttpRequestMessage(HttpMethod.Post, hubUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add(HmacSigner.SignatureHeader, signature);
        req.Headers.Add("X-Event-Type", "crm.contact.created");
        req.Headers.Add("X-Event-Id", eventId.ToString());

        try
        {
            using var response = await client.SendAsync(req);
            logger.LogInformation(
                "Webhook for contact {ContactId} sent to hub — HTTP {StatusCode}",
                contact.Id, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send webhook for contact {ContactId}", contact.Id);
        }
    }
}
