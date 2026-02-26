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
    private readonly Dictionary<int, decimal> _baseRates = new();

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

    /// <summary>
    /// Adds a Bank of England base rate for a tax year.
    /// </summary>
    public void AddBaseRate(int taxYear, decimal rate)
    {
        _baseRates[taxYear] = rate;
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

    /// <inheritdoc />
    public decimal? GetBaseRate(int taxYear)
    {
        return _baseRates.TryGetValue(taxYear, out var value)
            ? value
            : null;
    }

    // --- Factor table export (for CSV/JSON bridge to stochastic engine) ---

    /// <summary>
    /// Returns all S148 earnings revaluation factors as (taxYearOfEarnings, taxYearOfCalculation, percentage) tuples.
    /// </summary>
    public IReadOnlyList<(int TaxYearOfEarnings, int TaxYearOfCalculation, decimal Percentage)> GetAllEarningsFactors()
    {
        return _earningsFactors
            .Select(kv => (kv.Key.from, kv.Key.to, kv.Value))
            .OrderBy(x => x.from).ThenBy(x => x.to)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns all fixed revaluation rate bands as (fromYear, toYear, rate) tuples.
    /// </summary>
    public IReadOnlyList<(int FromYear, int ToYear, decimal Rate)> GetAllFixedRates()
    {
        return _fixedRates.AsReadOnly();
    }

    /// <summary>
    /// Returns all PIP increase factors as (method, taxYear, factor) tuples.
    /// </summary>
    public IReadOnlyList<(PipIncreaseMethod Method, int TaxYear, decimal Factor)> GetAllPipFactors()
    {
        return _pipFactors
            .Select(kv => (kv.Key.method, kv.Key.year, kv.Value))
            .OrderBy(x => x.method).ThenBy(x => x.year)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns all discount rates as (taxYear, rate) tuples.
    /// </summary>
    public IReadOnlyList<(int TaxYear, decimal Rate)> GetAllDiscountRates()
    {
        return _discountRates
            .Select(kv => (kv.Key, kv.Value))
            .OrderBy(x => x.Key)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns all base rates as (taxYear, rate) tuples.
    /// </summary>
    public IReadOnlyList<(int TaxYear, decimal Rate)> GetAllBaseRates()
    {
        return _baseRates
            .Select(kv => (kv.Key, kv.Value))
            .OrderBy(x => x.Key)
            .ToList()
            .AsReadOnly();
    }
}
