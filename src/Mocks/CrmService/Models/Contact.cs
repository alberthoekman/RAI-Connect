using System.ComponentModel.DataAnnotations;

namespace Rai.Mocks.CrmService.Models;

/// <summary>A CRM contact record.</summary>
public sealed class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(200)]
    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CreatedBy { get; set; }
}
