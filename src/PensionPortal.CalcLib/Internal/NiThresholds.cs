namespace PensionPortal.CalcLib.Internal;

/// <summary>
/// HMRC-published National Insurance thresholds and ONS average earnings index
/// for the contracted-out period 1978–1997. All values are fixed historical data.
/// Sources: HMRC "Rates and Allowances: NI contributions" via Royal London;
///          FRED/ONS Average Weekly Earnings Per Person (series AWEPPUKQ).
/// </summary>
internal static class NiThresholds
{
    /// <summary>
    /// Lower Earnings Limit by tax year (annual amount in £).
    /// Source: Royal London / HMRC NI contribution tables.
    /// </summary>
    internal static readonly IReadOnlyDictionary<int, decimal> LEL = new Dictionary<int, decimal>
    {
        [1978] = 910m,
        [1979] = 1014m,
        [1980] = 1196m,
        [1981] = 1404m,
        [1982] = 1534m,
        [1983] = 1690m,
        [1984] = 1768m,
        [1985] = 1846m,
        [1986] = 1976m,
        [1987] = 2028m,
        [1988] = 2132m,
        [1989] = 2236m,
        [1990] = 2392m,
        [1991] = 2704m,
        [1992] = 2808m,
        [1993] = 2912m,
        [1994] = 2964m,
        [1995] = 3016m,
        [1996] = 3172m,
        [1997] = 3224m,
    };

    /// <summary>
    /// Upper Earnings Limit by tax year (annual amount in £).
    /// Source: Royal London / HMRC NI contribution tables.
    /// </summary>
    internal static readonly IReadOnlyDictionary<int, decimal> UEL = new Dictionary<int, decimal>
    {
        [1978] = 6240m,
        [1979] = 7020m,
        [1980] = 8580m,
        [1981] = 10400m,
        [1982] = 11440m,
        [1983] = 12220m,
        [1984] = 13000m,
        [1985] = 13780m,
        [1986] = 14820m,
        [1987] = 15340m,
        [1988] = 15860m,
        [1989] = 16900m,
        [1990] = 18200m,
        [1991] = 20280m,
        [1992] = 21060m,
        [1993] = 21840m,
        [1994] = 22360m,
        [1995] = 22880m,
        [1996] = 23660m,
        [1997] = 24180m,
    };

    /// <summary>
    /// Average weekly earnings by tax year (£ per week, whole economy).
    /// Used to inflate/deflate the salary anchor from 1990 to other years.
    /// Source: FRED series AWEPPUKQ / ONS Average Weekly Earnings.
    /// </summary>
    internal static readonly IReadOnlyDictionary<int, decimal> AverageWeeklyEarnings = new Dictionary<int, decimal>
    {
        [1978] = 56.19m,
        [1979] = 63.89m,
        [1980] = 78.28m,
        [1981] = 91.53m,
        [1982] = 102.01m,
        [1983] = 110.82m,
        [1984] = 117.88m,
        [1985] = 127.87m,
        [1986] = 137.96m,
        [1987] = 148.33m,
        [1988] = 161.86m,
        [1989] = 177.65m,
        [1990] = 195.25m,
        [1991] = 212.29m,
        [1992] = 228.99m,
        [1993] = 235.71m,
        [1994] = 244.71m,
        [1995] = 253.91m,
        [1996] = 261.82m,
        [1997] = 272.93m,
    };
}
