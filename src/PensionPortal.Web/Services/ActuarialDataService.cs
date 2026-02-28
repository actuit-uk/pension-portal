using System.Data;
using Microsoft.Data.SqlClient;
using PensionPortal.CalcLib;

namespace PensionPortal.Web.Services;

/// <summary>
/// Loads actuarial factor data from the ActuarialData database
/// into CalcLib's DictionaryFactorProvider.
/// </summary>
public class ActuarialDataService
{
    private readonly string _connectionString;

    // Rate table IDs in ActuarialData
    private const int S148TableId = 120;
    private const int FixedRatesTableId = 130;
    private const int Lpi3TableId = 131;
    private const int Lpi5TableId = 132;
    private const int PublicSectorTableId = 133;
    private const int DiscountTableId = 128;

    public ActuarialDataService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("ActuarialData")
            ?? throw new InvalidOperationException("Connection string 'ActuarialData' not configured.");
    }

    /// <summary>
    /// Loads all GMP equalisation factors into a DictionaryFactorProvider.
    /// </summary>
    public DictionaryFactorProvider LoadFactors()
    {
        var provider = new DictionaryFactorProvider();

        var factors = LoadRateValues(S148TableId, FixedRatesTableId,
            Lpi3TableId, Lpi5TableId, PublicSectorTableId, DiscountTableId);

        // S148 earnings revaluation: age=earningsYear, term=calcYear, value=percentage
        foreach (DataRow row in factors[S148TableId].Rows)
        {
            provider.AddEarningsFactor(
                Convert.ToInt32(row["age"]),
                Convert.ToInt32(row["term"]),
                Convert.ToDecimal(row["value"]));
        }

        // Fixed revaluation rates: age=fromYear, value=rate (as decimal e.g. 0.085)
        // These are band start years — each rate applies until the next band
        var fixedRows = factors[FixedRatesTableId].Rows.Cast<DataRow>()
            .OrderBy(r => Convert.ToInt32(r["age"])).ToList();
        for (int i = 0; i < fixedRows.Count; i++)
        {
            int fromYear = Convert.ToInt32(fixedRows[i]["age"]);
            int toYear = i + 1 < fixedRows.Count
                ? Convert.ToInt32(fixedRows[i + 1]["age"]) - 1
                : 2060;
            decimal rate = Convert.ToDecimal(fixedRows[i]["value"]);
            provider.AddFixedRate(fromYear, toYear, rate);
        }

        // PIP increase factors: age=taxYear, value=rate (as decimal e.g. 0.03)
        foreach (DataRow row in factors[Lpi3TableId].Rows)
            provider.AddPipFactor(PipIncreaseMethod.LPI3,
                Convert.ToInt32(row["age"]), Convert.ToDecimal(row["value"]));

        foreach (DataRow row in factors[Lpi5TableId].Rows)
            provider.AddPipFactor(PipIncreaseMethod.LPI5,
                Convert.ToInt32(row["age"]), Convert.ToDecimal(row["value"]));

        foreach (DataRow row in factors[PublicSectorTableId].Rows)
            provider.AddPipFactor(PipIncreaseMethod.PublicSector,
                Convert.ToInt32(row["age"]), Convert.ToDecimal(row["value"]));

        // Discount rates: age=taxYear, value=rate (as decimal e.g. 0.025)
        foreach (DataRow row in factors[DiscountTableId].Rows)
            provider.AddDiscountRate(
                Convert.ToInt32(row["age"]), Convert.ToDecimal(row["value"]));

        return provider;
    }

    private Dictionary<int, DataTable> LoadRateValues(params int[] tableIds)
    {
        var paramNames = tableIds.Select((_, i) => $"@t{i}").ToArray();
        var sql = $"SELECT table_id, age, term, value FROM dbo.rate_value WHERE table_id IN ({string.Join(", ", paramNames)}) ORDER BY table_id, age, term";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < tableIds.Length; i++)
            cmd.Parameters.AddWithValue($"@t{i}", tableIds[i]);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        var allData = new DataTable();
        allData.Load(reader);

        // Split into per-table DataTables with matching schema
        var result = tableIds.ToDictionary(id => id, _ => allData.Clone());

        foreach (DataRow row in allData.Rows)
        {
            int tableId = Convert.ToInt32(row["table_id"]);
            if (result.TryGetValue(tableId, out var table))
                table.ImportRow(row);
        }

        return result;
    }
}
