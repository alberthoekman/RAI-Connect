using Microsoft.AspNetCore.Identity;

namespace Rai.Identity.Models;

/// <summary>Application user with an optional display name.</summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Human-readable display name shown in the UI.</summary>
    public string? DisplayName { get; set; }
}
