namespace PensionPortal.Web.Models;

/// <summary>
/// Defines a simulated user role for local testing.
/// In production, these would be derived from claims-based identity.
/// </summary>
public record RoleConfig(
    string Key,
    string DisplayName,
    string Description,
    string[]? SchemeIds = null,
    int? PersonId = null)
{
    /// <summary>
    /// Predefined roles for local development and testing.
    /// </summary>
    public static IReadOnlyList<RoleConfig> All { get; } = new[]
    {
        new RoleConfig("dbadmin", "DB Administrator",
            "Full access to all schemes and members."),

        new RoleConfig("sch001admin", "SCH001 Admin",
            "Manages the Sample Occupational Pension Scheme.",
            SchemeIds: new[] { "SCH001" }),

        new RoleConfig("consultant", "Consultant",
            "Advisory access to SCH001 and SCH002.",
            SchemeIds: new[] { "SCH001", "SCH002" }),

        new RoleConfig("sch004admin", "SCH004 Admin",
            "Simple Test Scheme — GMP equalisation test cases.",
            SchemeIds: new[] { "SCH004" }),

        new RoleConfig("member", "Member (John Smith)",
            "Can only see own membership records.",
            PersonId: 1),
    };

    public static RoleConfig? Find(string key) =>
        All.FirstOrDefault(r => r.Key == key);
}
