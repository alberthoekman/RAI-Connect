using System.ComponentModel.DataAnnotations;

namespace Rai.Mocks.TicketingService.Models;

/// <summary>A support ticket created from a CRM contact event.</summary>
public sealed class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The event id from the hub — used for idempotent dedup.</summary>
    public Guid SourceEventId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
