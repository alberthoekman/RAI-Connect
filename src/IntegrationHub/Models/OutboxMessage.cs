using System.ComponentModel.DataAnnotations;

namespace Rai.IntegrationHub.Models;

/// <summary>Represents a webhook event queued for reliable delivery to a target service.</summary>
public sealed class OutboxMessage
{
    /// <summary>Unique identifier (also used as idempotency key toward the target).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ISO-8601 UTC timestamp when the event was received.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Event type, e.g. <c>crm.contact.created</c>.</summary>
    [Required, MaxLength(100)]
    public string EventType { get; set; } = default!;

    /// <summary>JSON payload as received from the source.</summary>
    [Required]
    public string Payload { get; set; } = default!;

    /// <summary>Target URL to dispatch the event to.</summary>
    [Required, MaxLength(500)]
    public string TargetUrl { get; set; } = default!;

    /// <summary>Current delivery state.</summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>Number of delivery attempts made so far.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Earliest time the dispatcher should attempt (or re-attempt) delivery.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp of the last delivery attempt.</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>Last error message recorded by the dispatcher.</summary>
    [MaxLength(1000)]
    public string? LastError { get; set; }
}

public enum OutboxMessageStatus
{
    Pending,
    Delivered,
    DeadLettered,
}
