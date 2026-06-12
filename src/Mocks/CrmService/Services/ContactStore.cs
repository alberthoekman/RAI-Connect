using Rai.Mocks.CrmService.Models;
using System.Collections.Concurrent;

namespace Rai.Mocks.CrmService.Services;

/// <summary>In-memory store for CRM contacts (sufficient for the PoC demo).</summary>
public sealed class ContactStore
{
    private readonly ConcurrentBag<Contact> _contacts = [];

    /// <summary>Returns all contacts ordered newest first.</summary>
    public IReadOnlyList<Contact> GetAll() =>
        _contacts.OrderByDescending(c => c.CreatedAt).ToList();

    /// <summary>Adds a contact to the store.</summary>
    public void Add(Contact contact) => _contacts.Add(contact);
}
