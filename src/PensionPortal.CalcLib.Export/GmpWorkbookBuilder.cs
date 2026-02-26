using ClosedXML.Excel;
using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Export;

/// <summary>
/// Builds an Excel workbook from an equalisation result.
/// Produces a multi-sheet workbook with Summary, per-year audit trail,
/// GMP breakdowns, cash flow projection, and compensation schedule.
/// </summary>
public static class GmpWorkbookBuilder
{
    private const string NumberFormat2Dp = "#,##0.00";
    private const string NumberFormat3Dp = "#,##0.000";
    private const string PercentFormat = "0.0%";
    private const string IntFormat = "#,##0";

    /// <summary>
    /// Creates a complete workbook from an equalisation result.
    /// </summary>
    /// <param name="result">The full equalisation result to export.</param>
    /// <param name="memberName">Optional member name for the summary sheet.</param>
    public static XLWorkbook Build(EqualisationResult result, string? memberName = null)
    {
        var wb = new XLWorkbook();

        BuildSummarySheet(wb, result, memberName);
        BuildTaxYearDetailSheet(wb, result.Gmp);
        BuildGmpAtLeavingSheet(wb, result.Gmp);
        BuildGmpRevaluedSheet(wb, result.Gmp);
        BuildCashFlowSheet(wb, result);
        BuildCompensationSheet(wb, result);

        return wb;
    }

    /// <summary>
    /// Creates a workbook and saves it directly to a file path.
    /// </summary>
    public static void SaveToFile(EqualisationResult result, string filePath, string? memberName = null)
    {
        using var wb = Build(result, memberName);
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// Creates a workbook and writes it to a stream.
    /// </summary>
    public static void SaveToStream(EqualisationResult result, Stream stream, string? memberName = null)
    {
        using var wb = Build(result, memberName);
        wb.SaveAs(stream);
    }

    private static void BuildSummarySheet(XLWorkbook wb, EqualisationResult result, string? memberName)
    {
        var gmp = result.Gmp;
        var ws = wb.AddWorksheet("Summary");
        ws.TabColor = XLColor.DarkBlue;

        int row = 1;
        ws.Cell(row, 1).Value = "GMP Equalisation Summary";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Member info section
        if (memberName != null)
        {
            AddLabelValue(ws, row++, "Member", memberName);
        }
        AddLabelValue(ws, row++, "Revaluation Method", gmp.RevaluationMethod.ToString());
        AddLabelValue(ws, row++, "Tax Year of Leaving", $"{gmp.TaxYearOfLeaving}/{gmp.TaxYearOfLeaving + 1 - 2000:00}");
        AddLabelValue(ws, row++, "Working Life (Male)", gmp.WorkingLifeMale, IntFormat);
        AddLabelValue(ws, row++, "Working Life (Female)", gmp.WorkingLifeFemale, IntFormat);
        row++;

        // GMP at leaving
        ws.Cell(row, 1).Value = "GMP at Date of Leaving";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddHeaderRow(ws, row, "", "Male (pa)", "Male (pw)", "Female (pa)", "Female (pw)");
        row++;
        AddGmpRow(ws, row++, "Pre-88",
            gmp.MaleAtLeaving.Pre88Annual, gmp.MaleAtLeaving.Pre88Weekly,
            gmp.FemaleAtLeaving.Pre88Annual, gmp.FemaleAtLeaving.Pre88Weekly);
        AddGmpRow(ws, row++, "Post-88",
            gmp.MaleAtLeaving.Post88Annual, gmp.MaleAtLeaving.Post88Weekly,
            gmp.FemaleAtLeaving.Post88Annual, gmp.FemaleAtLeaving.Post88Weekly);
        AddGmpRow(ws, row++, "Total",
            gmp.MaleAtLeaving.TotalAnnual, gmp.MaleAtLeaving.TotalWeekly,
            gmp.FemaleAtLeaving.TotalAnnual, gmp.FemaleAtLeaving.TotalWeekly);
        row++;

        // Revaluation
        ws.Cell(row, 1).Value = "Revaluation";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        AddLabelValue(ws, row++, "Revaluation Factor (Male)", gmp.RevaluationFactorMale, NumberFormat3Dp);
        AddLabelValue(ws, row++, "Revaluation Factor (Female)", gmp.RevaluationFactorFemale, NumberFormat3Dp);
        row++;

        // Revalued GMP
        ws.Cell(row, 1).Value = "GMP Revalued to Payable Age";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddHeaderRow(ws, row, "", "Male (pa)", "Male (pw)", "Female (pa)", "Female (pw)");
        row++;
        AddGmpRow(ws, row++, "Pre-88",
            gmp.MaleRevalued.Pre88Annual, gmp.MaleRevalued.Pre88Weekly,
            gmp.FemaleRevalued.Pre88Annual, gmp.FemaleRevalued.Pre88Weekly);
        AddGmpRow(ws, row++, "Post-88",
            gmp.MaleRevalued.Post88Annual, gmp.MaleRevalued.Post88Weekly,
            gmp.FemaleRevalued.Post88Annual, gmp.FemaleRevalued.Post88Weekly);
        AddGmpRow(ws, row++, "Total",
            gmp.MaleRevalued.TotalAnnual, gmp.MaleRevalued.TotalWeekly,
            gmp.FemaleRevalued.TotalAnnual, gmp.FemaleRevalued.TotalWeekly);
        row++;

        // Barber window
        ws.Cell(row, 1).Value = "Barber Window";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        AddLabelValue(ws, row++, "GMP Proportion", gmp.BarberWindowProportion, PercentFormat);
        AddLabelValue(ws, row++, "Service Proportion", gmp.BarberServiceProportion, PercentFormat);
        row++;

        // Compensation totals
        ws.Cell(row, 1).Value = "Compensation";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        row++;
        AddLabelValue(ws, row++, "Total Compensation", result.TotalCompensation, NumberFormat2Dp);
        AddLabelValue(ws, row++, "Interest on Arrears", result.InterestOnArrears, NumberFormat2Dp);
        AddLabelValue(ws, row, "Total with Interest", result.TotalWithInterest, NumberFormat2Dp);
        ws.Cell(row, 2).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private static void BuildTaxYearDetailSheet(XLWorkbook wb, GmpResult result)
    {
        var ws = wb.AddWorksheet("Tax Year Detail");
        ws.TabColor = XLColor.ForestGreen;

        // Header row
        string[] headers = {
            "Tax Year", "Earnings/NICs", "Is NICs", "Is Pre-88",
            "Divisor", "Accrual Rate", "S148 Factor %",
            "Revalued Earnings", "Raw GMP Male", "Raw GMP Female"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        int row = 2;
        foreach (var d in result.TaxYearDetails)
        {
            ws.Cell(row, 1).Value = $"{d.TaxYear}/{d.TaxYear + 1 - 2000:00}";
            ws.Cell(row, 2).Value = (double)d.EarningsOrNICs;
            ws.Cell(row, 2).Style.NumberFormat.Format = NumberFormat2Dp;
            ws.Cell(row, 3).Value = d.IsNICs ? "Yes" : "No";
            ws.Cell(row, 4).Value = d.IsPre88 ? "Yes" : "No";
            ws.Cell(row, 5).Value = (double)d.Divisor;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.0000";
            ws.Cell(row, 6).Value = (double)d.AccrualRate;
            ws.Cell(row, 6).Style.NumberFormat.Format = PercentFormat;
            ws.Cell(row, 7).Value = (double)d.S148FactorPct;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.0";
            ws.Cell(row, 8).Value = (double)d.RevaluedEarnings;
            ws.Cell(row, 8).Style.NumberFormat.Format = IntFormat;
            ws.Cell(row, 9).Value = (double)d.RawGmpMale;
            ws.Cell(row, 9).Style.NumberFormat.Format = NumberFormat2Dp;
            ws.Cell(row, 10).Value = (double)d.RawGmpFemale;
            ws.Cell(row, 10).Style.NumberFormat.Format = NumberFormat2Dp;
            row++;
        }

        // Totals row
        int lastDataRow = row - 1;
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).FormulaA1 = $"SUM(B2:B{lastDataRow})";
        ws.Cell(row, 2).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 9).FormulaA1 = $"SUM(I2:I{lastDataRow})";
        ws.Cell(row, 9).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Cell(row, 10).FormulaA1 = $"SUM(J2:J{lastDataRow})";
        ws.Cell(row, 10).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 10).Style.Font.Bold = true;

        // Create Excel table
        var tableRange = ws.Range(1, 1, lastDataRow, headers.Length);
        var table = tableRange.CreateTable("TaxYearDetail");
        table.Theme = XLTableTheme.TableStyleLight9;

        // Freeze header row
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void BuildGmpAtLeavingSheet(XLWorkbook wb, GmpResult result)
    {
        var ws = wb.AddWorksheet("GMP at Leaving");
        ws.TabColor = XLColor.DarkOrange;

        string[] headers = { "", "Pre-88 Annual", "Post-88 Annual", "Total Annual",
                             "Pre-88 Weekly", "Post-88 Weekly", "Total Weekly" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        WriteBreakdownRow(ws, 2, "Male", result.MaleAtLeaving);
        WriteBreakdownRow(ws, 3, "Female", result.FemaleAtLeaving);

        ws.Columns().AdjustToContents();
    }

    private static void BuildGmpRevaluedSheet(XLWorkbook wb, GmpResult result)
    {
        var ws = wb.AddWorksheet("GMP Revalued");
        ws.TabColor = XLColor.DarkRed;

        // Revaluation info
        ws.Cell(1, 1).Value = "Revaluation Method";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = result.RevaluationMethod.ToString();
        ws.Cell(2, 1).Value = "Factor (Male)";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 2).Value = (double)result.RevaluationFactorMale;
        ws.Cell(2, 2).Style.NumberFormat.Format = NumberFormat3Dp;
        ws.Cell(3, 1).Value = "Factor (Female)";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 2).Value = (double)result.RevaluationFactorFemale;
        ws.Cell(3, 2).Style.NumberFormat.Format = NumberFormat3Dp;

        int row = 5;
        string[] headers = { "", "Pre-88 Annual", "Post-88 Annual", "Total Annual",
                             "Pre-88 Weekly", "Post-88 Weekly", "Total Weekly" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        WriteBreakdownRow(ws, row + 1, "Male", result.MaleRevalued);
        WriteBreakdownRow(ws, row + 2, "Female", result.FemaleRevalued);

        ws.Columns().AdjustToContents();
    }

    private static void BuildCashFlowSheet(XLWorkbook wb, EqualisationResult result)
    {
        var ws = wb.AddWorksheet("Cash Flow");
        ws.TabColor = XLColor.Teal;

        string[] headers = {
            "Tax Year",
            "Status (M)", "Pre-88 GMP (M)", "Post-88 GMP (M)", "Total GMP (M)", "Excess (M)", "Total Pension (M)",
            "Status (F)", "Pre-88 GMP (F)", "Post-88 GMP (F)", "Total GMP (F)", "Excess (F)", "Total Pension (F)",
            "Post-88 Inc %", "Excess Inc %"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var cf in result.CashFlow)
        {
            ws.Cell(row, 1).Value = $"{cf.TaxYear}/{cf.TaxYear + 1 - 2000:00}";
            ws.Cell(row, 2).Value = cf.StatusMale.ToString();
            WriteCurrency(ws, row, 3, cf.Pre88GmpMale);
            WriteCurrency(ws, row, 4, cf.Post88GmpMale);
            WriteCurrency(ws, row, 5, cf.TotalGmpMale);
            WriteCurrency(ws, row, 6, cf.ExcessMale);
            WriteCurrency(ws, row, 7, cf.TotalPensionMale);
            ws.Cell(row, 8).Value = cf.StatusFemale.ToString();
            WriteCurrency(ws, row, 9, cf.Pre88GmpFemale);
            WriteCurrency(ws, row, 10, cf.Post88GmpFemale);
            WriteCurrency(ws, row, 11, cf.TotalGmpFemale);
            WriteCurrency(ws, row, 12, cf.ExcessFemale);
            WriteCurrency(ws, row, 13, cf.TotalPensionFemale);
            WritePercent(ws, row, 14, cf.Post88GmpIncFactor);
            WritePercent(ws, row, 15, cf.ExcessIncFactor);
            row++;
        }

        // Create Excel table
        int lastDataRow = row - 1;
        if (lastDataRow >= 2)
        {
            var tableRange = ws.Range(1, 1, lastDataRow, headers.Length);
            var table = tableRange.CreateTable("CashFlow");
            table.Theme = XLTableTheme.TableStyleLight16;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void BuildCompensationSheet(XLWorkbook wb, EqualisationResult result)
    {
        var ws = wb.AddWorksheet("Compensation");
        ws.TabColor = XLColor.Purple;

        string[] headers = {
            "Tax Year", "Actual Cash Flow", "Opp Sex Cash Flow",
            "Compensation", "Discount Rate", "Discount Factor"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var c in result.Compensation)
        {
            ws.Cell(row, 1).Value = $"{c.TaxYear}/{c.TaxYear + 1 - 2000:00}";
            WriteCurrency(ws, row, 2, c.ActualCashFlow);
            WriteCurrency(ws, row, 3, c.OppSexCashFlow);
            WriteCurrency(ws, row, 4, c.CompensationCashFlow);
            WritePercent(ws, row, 5, c.DiscountRate);
            ws.Cell(row, 6).Value = (double)c.DiscountFactor;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.000000";
            row++;
        }

        // Totals row
        int lastDataRow = row - 1;
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).FormulaA1 = $"SUM(D2:D{lastDataRow})";
        ws.Cell(row, 4).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 4).Style.Font.Bold = true;
        row += 2;

        // Interest and grand total
        AddLabelValue(ws, row++, "Interest on Arrears", result.InterestOnArrears, NumberFormat2Dp);
        AddLabelValue(ws, row, "Total with Interest", result.TotalWithInterest, NumberFormat2Dp);
        ws.Cell(row, 2).Style.Font.Bold = true;

        // Create Excel table
        if (lastDataRow >= 2)
        {
            var tableRange = ws.Range(1, 1, lastDataRow, headers.Length);
            var table = tableRange.CreateTable("Compensation");
            table.Theme = XLTableTheme.TableStyleLight21;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void AddLabelValue(IXLWorksheet ws, int row, string label, string value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value;
    }

    private static void AddLabelValue(IXLWorksheet ws, int row, string label, decimal value, string format)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = (double)value;
        ws.Cell(row, 2).Style.NumberFormat.Format = format;
    }

    private static void AddHeaderRow(IXLWorksheet ws, int row, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
    }

    private static void AddGmpRow(IXLWorksheet ws, int row, string label,
        decimal maleAnnual, decimal maleWeekly, decimal femaleAnnual, decimal femaleWeekly)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = (double)maleAnnual;
        ws.Cell(row, 2).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 3).Value = (double)maleWeekly;
        ws.Cell(row, 3).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 4).Value = (double)femaleAnnual;
        ws.Cell(row, 4).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 5).Value = (double)femaleWeekly;
        ws.Cell(row, 5).Style.NumberFormat.Format = NumberFormat2Dp;
    }

    private static void WriteBreakdownRow(IXLWorksheet ws, int row, string label, GmpBreakdown b)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = (double)b.Pre88Annual;
        ws.Cell(row, 2).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 3).Value = (double)b.Post88Annual;
        ws.Cell(row, 3).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 4).Value = (double)b.TotalAnnual;
        ws.Cell(row, 4).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 5).Value = (double)b.Pre88Weekly;
        ws.Cell(row, 5).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 6).Value = (double)b.Post88Weekly;
        ws.Cell(row, 6).Style.NumberFormat.Format = NumberFormat2Dp;
        ws.Cell(row, 7).Value = (double)b.TotalWeekly;
        ws.Cell(row, 7).Style.NumberFormat.Format = NumberFormat2Dp;
    }

    private static void WriteCurrency(IXLWorksheet ws, int row, int col, decimal value)
    {
        ws.Cell(row, col).Value = (double)value;
        ws.Cell(row, col).Style.NumberFormat.Format = NumberFormat2Dp;
    }

    private static void WritePercent(IXLWorksheet ws, int row, int col, decimal value)
    {
        ws.Cell(row, col).Value = (double)value;
        ws.Cell(row, col).Style.NumberFormat.Format = PercentFormat;
    }
}
