using Rai.Mocks.TicketingService.Models;
using System.Collections.Concurrent;

namespace Rai.Mocks.TicketingService.Services;

/// <summary>In-memory store for tickets and processed event ids (idempotency).</summary>
public sealed class TicketStore
{
    private readonly ConcurrentBag<Ticket> _tickets = [];
    private readonly ConcurrentDictionary<Guid, bool> _processedEvents = new();

    /// <summary>Returns all tickets ordered newest first.</summary>
    public IReadOnlyList<Ticket> GetAll() =>
        _tickets.OrderByDescending(t => t.CreatedAt).ToList();

    /// <summary>Adds a ticket. Returns <c>false</c> if the event was already processed (duplicate).</summary>
    public bool TryAdd(Ticket ticket)
    {
        if (!_processedEvents.TryAdd(ticket.SourceEventId, true))
            return false;
        _tickets.Add(ticket);
        return true;
    }
}
