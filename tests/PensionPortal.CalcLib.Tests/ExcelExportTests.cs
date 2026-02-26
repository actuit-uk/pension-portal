using PensionPortal.CalcLib;
using PensionPortal.CalcLib.Export;
using PensionPortal.CalcLib.Internal;
using PensionPortal.CalcLib.Tests.TestData;

namespace PensionPortal.CalcLib.Tests;

public class ExcelExportTests
{
    [Fact]
    public void Case4_ExportsWithoutError()
    {
        var result = GmpCalculator.Calculate(
            Case4Data.Member, Case4Data.Scheme, Case4Data.CreateFactors(),
            settlementDate: new DateTime(2025, 12, 1));

        using var wb = GmpWorkbookBuilder.Build(result, "Case 4 — Male, S148");

        Assert.Equal(6, wb.Worksheets.Count);
        Assert.NotNull(wb.Worksheet("Summary"));
        Assert.NotNull(wb.Worksheet("Cash Flow"));
        Assert.NotNull(wb.Worksheet("Compensation"));

        // Save to temp for manual inspection
        var path = Path.Combine(Path.GetTempPath(), "Case4_Equalisation.xlsx");
        wb.SaveAs(path);
    }

    [Fact]
    public void PasaExample5_ExportsWithoutError()
    {
        var gmp = PasaExample5Data.CreateGmpResult();
        var factors = PasaExample5Data.CreateFactors();
        var rawCashFlow = CashFlowBuilder.Build(
            gmp, PasaExample5Data.Member, PasaExample5Data.Scheme, factors);

        var (compensation, total) = CompensationCalculator.Calculate(
            rawCashFlow, PasaExample5Data.Member.Sex, PasaExample5Data.Scheme, factors,
            gmp.BarberWindowProportion, gmp.BarberServiceProportion);

        var result = new EqualisationResult(
            Gmp: gmp,
            CashFlow: rawCashFlow,
            Compensation: compensation,
            TotalCompensation: total);

        using var wb = GmpWorkbookBuilder.Build(result, "PASA Example 5 — Male, Deferred");

        Assert.Equal(6, wb.Worksheets.Count);

        // Save to temp for manual inspection
        var path = Path.Combine(Path.GetTempPath(), "PasaExample5_Equalisation.xlsx");
        wb.SaveAs(path);
    }
}
