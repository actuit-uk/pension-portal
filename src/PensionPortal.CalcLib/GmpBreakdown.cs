namespace PensionPortal.CalcLib;

/// <summary>
/// GMP amounts for a single sex, split into pre-1988 and post-1988 components,
/// expressed both as annual and weekly amounts.
/// </summary>
/// <param name="Pre88Annual">Pre-April 1988 GMP per annum.</param>
/// <param name="Post88Annual">Post-April 1988 GMP per annum.</param>
/// <param name="TotalAnnual">Total GMP per annum (pre + post 88).</param>
/// <param name="Pre88Weekly">Pre-April 1988 GMP per week.</param>
/// <param name="Post88Weekly">Post-April 1988 GMP per week.</param>
/// <param name="TotalWeekly">Total GMP per week (pre + post 88).</param>
public record GmpBreakdown(
    decimal Pre88Annual,
    decimal Post88Annual,
    decimal TotalAnnual,
    decimal Pre88Weekly,
    decimal Post88Weekly,
    decimal TotalWeekly);
