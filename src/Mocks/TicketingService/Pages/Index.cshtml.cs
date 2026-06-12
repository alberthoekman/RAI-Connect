using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rai.Mocks.TicketingService.Models;
using Rai.Mocks.TicketingService.Services;

namespace Rai.Mocks.TicketingService.Pages;

/// <summary>Protected home page — proves SSO (no second login required).</summary>
[Authorize]
public sealed class IndexModel(TicketStore store) : PageModel
{
    public string UserName { get; private set; } = string.Empty;
    public IReadOnlyList<Ticket> Tickets { get; private set; } = [];

    public void OnGet()
    {
        UserName = User.FindFirst("name")?.Value
                   ?? User.Identity?.Name
                   ?? "Unknown";
        Tickets = store.GetAll();
    }
}
