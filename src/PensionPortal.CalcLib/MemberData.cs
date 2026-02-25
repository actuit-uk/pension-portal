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
public record MemberData(
    Sex Sex,
    DateTime DateOfBirth,
    DateTime DateCOStart,
    DateTime DateCOEnd,
    DateTime DateOfLeaving,
    DateTime? DateOfRetirement,
    DateTime? DateOfDeath,
    IReadOnlyDictionary<int, decimal> Earnings);
