namespace PensionPortal.CalcLib;

/// <summary>
/// The phase of GMP entitlement for a given tax year.
/// </summary>
public enum GmpStatus
{
    /// <summary>
    /// Tax year of leaving contracted-out employment.
    /// GMP is calculated at leaving values.
    /// </summary>
    Exit,

    /// <summary>
    /// Deferred: after leaving, before GMP payable age.
    /// GMP amounts are fixed (no increases applied).
    /// </summary>
    Deferred,

    /// <summary>
    /// Pension In Payment: from GMP payable age onwards.
    /// Pre-88 GMP stays flat; post-88 GMP increases annually at LPI.
    /// </summary>
    InPayment
}
