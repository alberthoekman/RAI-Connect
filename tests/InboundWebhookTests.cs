using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Models;
using Rai.Shared.Hmac;

namespace Rai.Tests;

public sealed class InboundWebhookTests(HubWebFactory factory) : IClassFixture<HubWebFactory>
{
    private const string InboundSecret = "test-inbound-secret";
    private const string Payload = """{"id":1,"name":"Test Contact"}""";
    private const string ContentType = "application/json";

    private static HttpContent MakeContent(string body) =>
        new StringContent(body, Encoding.UTF8, ContentType);

    private HttpRequestMessage BuildRequest(string body, string? secret = InboundSecret, Guid? eventId = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/crm")
        {
            Content = MakeContent(body),
        };
        if (secret is not null)
            req.Headers.Add(HmacSigner.SignatureHeader, HmacSigner.Sign(body, secret));
        if (eventId.HasValue)
            req.Headers.Add("X-Event-Id", eventId.Value.ToString());
        req.Headers.Add("X-Event-Type", "crm.contact.created");
        return req;
    }

    [Fact]
    public async Task Post_WithValidHmac_Returns200_AndCreatesOutboxEntry()
    {
        var client = factory.CreateClient();
        var eventId = Guid.NewGuid();

        using var req = BuildRequest(Payload, InboundSecret, eventId);
        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the outbox entry was persisted
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        var msg = await db.OutboxMessages.FindAsync(eventId);
        Assert.NotNull(msg);
        Assert.Equal(OutboxMessageStatus.Pending, msg.Status);
        Assert.Equal("crm.contact.created", msg.EventType);
    }

    [Fact]
    public async Task Post_WithInvalidHmac_Returns401()
    {
        var client = factory.CreateClient();

        using var req = BuildRequest(Payload, secret: "wrong-secret");
        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingHmacHeader_Returns401()
    {
        var client = factory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/crm")
        {
            Content = MakeContent(Payload),
        };
        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateEventId_Returns200_WithDuplicateStatus()
    {
        var client = factory.CreateClient();
        var eventId = Guid.NewGuid();

        // First delivery
        using var req1 = BuildRequest(Payload, InboundSecret, eventId);
        var first = await client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Duplicate — same event id
        using var req2 = BuildRequest(Payload, InboundSecret, eventId);
        var second = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("duplicate", body?["status"]);
    }
}
