namespace PensionPortal.CalcLib;

/// <summary>
/// Per-member input data for GMP calculation.
/// Maps to tblCalculationInput in the legacy GMPEQ database.
/// </summary>
/// <param name="Sex">Member's sex — determines GMP payable age (60F / 65M).</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="DateCOStart">Date contracted-out employment started.</param>
/// <param name="DateCOEnd">Date contracted-out employment ended.</param>
/// <param name="DateOfLeaving">Date of leaving pensionable service.</param>
/// <param name="DateOfRetirement">Date of retirement, if known.</param>
/// <param name="DateOfDeath">Date of death, if applicable.</param>
/// <param name="Earnings">
/// Earnings by tax year. Key is the tax year (e.g. 1987 = 1987/88).
/// For tax years up to 1987: contracted-out NI contributions.
/// For tax years 1988–1997: band earnings.
/// </param>
/// <param name="PensionAtLeaving">
/// Total scheme pension at date of leaving (per annum), if known.
/// Tier 1 for excess calculation: excess = PensionAtLeaving - TotalGMP.
/// </param>
/// <param name="FinalPensionableSalary">
/// Final pensionable salary at leaving, if known.
/// Tier 2 fallback for excess calculation when PensionAtLeaving is not available:
/// estimated total pension = salary × pensionable service ÷ accrual denominator.
/// </param>
/// <param name="HasTransferredInGmp">
/// Set to true if the member has GMP transferred in from another scheme.
/// Transferred-in GMP requires separate revaluation, contracted-out periods, and
/// comparator construction that CalcLib does not currently model. When true, a
/// warning is included on the EqualisationResult.
/// </param>
public record MemberData(
    Sex Sex,
    DateTime DateOfBirth,
    DateTime DateCOStart,
    DateTime DateCOEnd,
    DateTime DateOfLeaving,
    DateTime? DateOfRetirement,
    DateTime? DateOfDeath,
    IReadOnlyDictionary<int, decimal> Earnings,
    decimal? PensionAtLeaving = null,
    decimal? FinalPensionableSalary = null,
    bool HasTransferredInGmp = false);
