using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Rai.IntegrationHub.Data;
using Rai.IntegrationHub.Dispatch;
using Rai.IntegrationHub.Models;

namespace Rai.Tests;

/// <summary>
/// Unit tests for <see cref="WebhookDispatcher"/> delivery logic.
/// Each test gets an isolated in-memory SQLite database and a mock HTTP handler
/// that simulates success or failure responses.
/// </summary>
public sealed class DispatcherTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HubDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public DispatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new HubDbContext(options);
        _db.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddDbContext<HubDbContext>(o => o.UseSqlite(_connection));
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    }

    private static IConfiguration BuildConfig(string outboundSecret = "out-secret") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:OutboundHmacSecret"] = outboundSecret,
            })
            .Build();

    private static WebhookDispatcher BuildDispatcher(
        IServiceScopeFactory scopeFactory,
        HttpMessageHandler httpHandler)
    {
        var httpClientFactory = new MockHttpClientFactory(httpHandler);
        return new WebhookDispatcher(
            scopeFactory,
            httpClientFactory,
            BuildConfig(),
            NullLogger<WebhookDispatcher>.Instance);
    }

    private async Task<OutboxMessage> SeedPendingMessage(DateTimeOffset? nextAttemptAt = null)
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "test.event",
            Payload = """{"x":1}""",
            TargetUrl = "http://localhost:9999/hook",
            NextAttemptAt = nextAttemptAt ?? DateTimeOffset.UtcNow.AddSeconds(-1),
        };
        _db.OutboxMessages.Add(msg);
        await _db.SaveChangesAsync();
        return msg;
    }

    [Fact]
    public async Task Dispatch_SetsDelivered_WhenTargetReturns200()
    {
        var msg = await SeedPendingMessage();
        var dispatcher = BuildDispatcher(_scopeFactory, new FakeHandler(HttpStatusCode.OK));

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        await _db.Entry(msg).ReloadAsync();
        Assert.Equal(OutboxMessageStatus.Delivered, msg.Status);
        Assert.Equal(1, msg.AttemptCount);
    }

    [Fact]
    public async Task Dispatch_SchedulesRetry_WhenTargetReturns500()
    {
        var msg = await SeedPendingMessage();
        var dispatcher = BuildDispatcher(_scopeFactory, new FakeHandler(HttpStatusCode.InternalServerError));

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        await _db.Entry(msg).ReloadAsync();
        Assert.Equal(OutboxMessageStatus.Pending, msg.Status);
        Assert.Equal(1, msg.AttemptCount);
        Assert.NotNull(msg.NextAttemptAt);
        Assert.True(msg.NextAttemptAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Dispatch_DeadLetters_AfterMaxAttempts()
    {
        // Seed a message that already has 4 failed attempts
        var msg = await SeedPendingMessage();
        msg.AttemptCount = 4;
        await _db.SaveChangesAsync();

        var dispatcher = BuildDispatcher(_scopeFactory, new FakeHandler(HttpStatusCode.ServiceUnavailable));

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        await _db.Entry(msg).ReloadAsync();
        Assert.Equal(OutboxMessageStatus.DeadLettered, msg.Status);
        Assert.Equal(5, msg.AttemptCount);
    }

    [Fact]
    public async Task Dispatch_SkipsMessages_ScheduledInFuture()
    {
        var msg = await SeedPendingMessage(nextAttemptAt: DateTimeOffset.UtcNow.AddHours(1));
        var dispatcher = BuildDispatcher(_scopeFactory, new FakeHandler(HttpStatusCode.OK));

        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        await _db.Entry(msg).ReloadAsync();
        Assert.Equal(OutboxMessageStatus.Pending, msg.Status);
        Assert.Equal(0, msg.AttemptCount);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Returns a fixed status code for all requests.</summary>
file sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(statusCode));
}

/// <summary>Wraps a handler so the dispatcher's named HTTP client resolves to it.</summary>
file sealed class MockHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler);
}
