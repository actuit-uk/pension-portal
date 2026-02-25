namespace PensionPortal.CalcLib;

/// <summary>
/// Pension Increase (Pensions in Payment) method.
/// Determines which increase table applies to excess pension.
/// </summary>
public enum PipIncreaseMethod
{
    /// <summary>Public sector pension increase rates.</summary>
    PublicSector,

    /// <summary>Limited Price Indexation capped at 3%.</summary>
    LPI3,

    /// <summary>Limited Price Indexation capped at 5%.</summary>
    LPI5
}
