namespace Rai.Identity.Authorization;

/// <summary>Canonical application permission name constants used by policies and seed data.</summary>
public static class AppPermissions
{
    public const string UsersRead     = "users:read";
    public const string UsersWrite    = "users:write";
    public const string RolesManage   = "roles:manage";
    public const string ContactsWrite = "contacts:write";

    /// <summary>All well-known permissions used during seed.</summary>
    public static readonly IReadOnlyList<(string Name, string Description)> All =
    [
        (UsersRead,     "View the user list"),
        (UsersWrite,    "Create and edit users"),
        (RolesManage,   "Create roles and assign permissions"),
        (ContactsWrite, "Create CRM contacts"),
    ];
}
