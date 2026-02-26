using ClosedXML.Excel;
using PensionPortal.CalcLib;

namespace PensionPortal.CalcLib.Export;

/// <summary>
/// Builds an Excel workbook from a GMP calculation result.
/// Produces a multi-sheet workbook with Summary, per-year audit trail,
/// and revalued GMP breakdown — following the appcore pattern of
/// formatted tables with frozen panes and type-aware number formats.
/// </summary>
public static class GmpWorkbookBuilder
{
    private const string NumberFormat2Dp = "#,##0.00";
    private const string NumberFormat3Dp = "#,##0.000";
    private const string PercentFormat = "0.0%";
    private const string IntFormat = "#,##0";

    /// <summary>
    /// Creates a complete workbook from a GMP result.
    /// </summary>
    /// <param name="result">The GMP calculation result to export.</param>
    /// <param name="memberName">Optional member name for the summary sheet.</param>
    public static XLWorkbook Build(GmpResult result, string? memberName = null)
    {
        var wb = new XLWorkbook();

        BuildSummarySheet(wb, result, memberName);
        BuildTaxYearDetailSheet(wb, result);
        BuildGmpAtLeavingSheet(wb, result);
        BuildGmpRevaluedSheet(wb, result);

        return wb;
    }

    /// <summary>
    /// Creates a workbook and saves it directly to a file path.
    /// </summary>
    public static void SaveToFile(GmpResult result, string filePath, string? memberName = null)
    {
        using var wb = Build(result, memberName);
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// Creates a workbook and writes it to a stream.
    /// </summary>
    public static void SaveToStream(GmpResult result, Stream stream, string? memberName = null)
    {
        using var wb = Build(result, memberName);
        wb.SaveAs(stream);
    }

    private static void BuildSummarySheet(XLWorkbook wb, GmpResult result, string? memberName)
    {
        var ws = wb.AddWorksheet("Summary");
        ws.TabColor = XLColor.DarkBlue;

        int row = 1;
        ws.Cell(row, 1).Value = "GMP Calculation Summary";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Member info section
        if (memberName != null)
        {
            AddLabelValue(ws, row++, "Member", memberName);
        }
        AddLabelValue(ws, row++, "Revaluation Method", result.RevaluationMethod.ToString());
        AddLabelValue(ws, row++, "Tax Year of Leaving", $"{result.TaxYearOfLeaving}/{result.TaxYearOfLeaving + 1 - 2000:00}");
        AddLabelValue(ws, row++, "Working Life (Male)", result.WorkingLifeMale, IntFormat);
        AddLabelValue(ws, row++, "Working Life (Female)", result.WorkingLifeFemale, IntFormat);
        row++;

        // GMP at leaving
        ws.Cell(row, 1).Value = "GMP at Date of Leaving";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddHeaderRow(ws, row, "", "Male (pa)", "Male (pw)", "Female (pa)", "Female (pw)");
        row++;
        AddGmpRow(ws, row++, "Pre-88",
            result.MaleAtLeaving.Pre88Annual, result.MaleAtLeaving.Pre88Weekly,
            result.FemaleAtLeaving.Pre88Annual, result.FemaleAtLeaving.Pre88Weekly);
        AddGmpRow(ws, row++, "Post-88",
            result.MaleAtLeaving.Post88Annual, result.MaleAtLeaving.Post88Weekly,
            result.FemaleAtLeaving.Post88Annual, result.FemaleAtLeaving.Post88Weekly);
        AddGmpRow(ws, row++, "Total",
            result.MaleAtLeaving.TotalAnnual, result.MaleAtLeaving.TotalWeekly,
            result.FemaleAtLeaving.TotalAnnual, result.FemaleAtLeaving.TotalWeekly);
        row++;

        // Revaluation
        ws.Cell(row, 1).Value = "Revaluation";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        AddLabelValue(ws, row++, "Revaluation Factor (Male)", result.RevaluationFactorMale, NumberFormat3Dp);
        AddLabelValue(ws, row++, "Revaluation Factor (Female)", result.RevaluationFactorFemale, NumberFormat3Dp);
        row++;

        // Revalued GMP
        ws.Cell(row, 1).Value = "GMP Revalued to Payable Age";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddHeaderRow(ws, row, "", "Male (pa)", "Male (pw)", "Female (pa)", "Female (pw)");
        row++;
        AddGmpRow(ws, row++, "Pre-88",
            result.MaleRevalued.Pre88Annual, result.MaleRevalued.Pre88Weekly,
            result.FemaleRevalued.Pre88Annual, result.FemaleRevalued.Pre88Weekly);
        AddGmpRow(ws, row++, "Post-88",
            result.MaleRevalued.Post88Annual, result.MaleRevalued.Post88Weekly,
            result.FemaleRevalued.Post88Annual, result.FemaleRevalued.Post88Weekly);
        AddGmpRow(ws, row++, "Total",
            result.MaleRevalued.TotalAnnual, result.MaleRevalued.TotalWeekly,
            result.FemaleRevalued.TotalAnnual, result.FemaleRevalued.TotalWeekly);

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
}
