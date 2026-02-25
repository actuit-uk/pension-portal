namespace PensionPortal.CalcLib;

/// <summary>
/// In-memory factor provider backed by dictionaries.
/// Useful for testing and for loading factor data from any source
/// (database, CSV, Excel) into a simple lookup structure.
/// </summary>
public class DictionaryFactorProvider : IFactorProvider
{
    private readonly Dictionary<(int from, int to), decimal> _earningsFactors = new();
    private readonly List<(int fromYear, int toYear, decimal rate)> _fixedRates = new();
    private readonly Dictionary<(PipIncreaseMethod method, int year), decimal> _pipFactors = new();
    private readonly Dictionary<int, decimal> _discountRates = new();

    /// <summary>
    /// Adds an earnings revaluation factor.
    /// </summary>
    public void AddEarningsFactor(int taxYearOfEarnings, int taxYearOfCalculation, decimal percentage)
    {
        _earningsFactors[(taxYearOfEarnings, taxYearOfCalculation)] = percentage;
    }

    /// <summary>
    /// Adds a fixed revaluation rate band.
    /// Terminations in tax years fromYear to toYear use the given rate.
    /// </summary>
    public void AddFixedRate(int fromYear, int toYear, decimal rate)
    {
        _fixedRates.Add((fromYear, toYear, rate));
    }

    /// <summary>
    /// Adds a pension increase factor.
    /// </summary>
    public void AddPipFactor(PipIncreaseMethod method, int taxYear, decimal factor)
    {
        _pipFactors[(method, taxYear)] = factor;
    }

    /// <summary>
    /// Adds a discount rate.
    /// </summary>
    public void AddDiscountRate(int taxYear, decimal rate)
    {
        _discountRates[taxYear] = rate;
    }

    /// <inheritdoc />
    public decimal? GetEarningsRevaluationFactor(int taxYearOfEarnings, int taxYearOfCalculation)
    {
        return _earningsFactors.TryGetValue((taxYearOfEarnings, taxYearOfCalculation), out var value)
            ? value
            : null;
    }

    /// <inheritdoc />
    public decimal GetFixedRevaluationRate(int taxYearOfTermination)
    {
        foreach (var (fromYear, toYear, rate) in _fixedRates)
        {
            if (taxYearOfTermination >= fromYear && taxYearOfTermination <= toYear)
                return rate;
        }

        // Default to the most recent band rate if no match
        throw new ArgumentException(
            $"No fixed revaluation rate defined for tax year {taxYearOfTermination}.");
    }

    /// <inheritdoc />
    public decimal? GetPipIncreaseFactor(PipIncreaseMethod method, int taxYear)
    {
        return _pipFactors.TryGetValue((method, taxYear), out var value)
            ? value
            : null;
    }

    /// <inheritdoc />
    public decimal? GetDiscountRate(int taxYear)
    {
        return _discountRates.TryGetValue(taxYear, out var value)
            ? value
            : null;
    }
}
