namespace PensionPortal.CalcLib;

/// <summary>
/// How pension increases are applied once in payment.
/// Determines whether GMP and excess pension increase independently
/// or whether a single rate applies to the total pension with a GMP floor test.
/// </summary>
public enum PensionIncreaseMethod
{
    /// <summary>
    /// Separate increase method: each component increases independently.
    /// Pre-88 GMP stays flat, post-88 GMP increases at LPI3 (statutory),
    /// excess pension increases at the scheme PIP rate.
    /// Anti-franking is a no-op under this method because excess is never reduced.
    /// </summary>
    Separate,

    /// <summary>
    /// Overall increase method: the scheme applies one rate to the total pension.
    /// The GMP floor is then tested: pre-88 GMP (flat) + post-88 GMP (LPI3 statutory).
    /// If the total pension falls below the GMP floor, it is topped up.
    /// Excess pension is the residual (total pension minus GMP components)
    /// and can erode over time if the scheme rate is lower than the effective
    /// GMP increase rate. Anti-franking becomes material under this method.
    /// </summary>
    Overall
}
