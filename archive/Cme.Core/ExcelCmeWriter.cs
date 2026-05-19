using ClosedXML.Excel;

namespace Cme.Core;

/// <summary>
/// Writes CME metrics to Excel files.
/// </summary>
public class ExcelCmeWriter
{
    public void WriteResults(
        string outputPath,
        List<EegWindowRecord> windows,
        List<SessionMetrics> sessionMetrics,
        GlobalMetrics globalMetrics,
        double normalizationFactor)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Windows with CME
        WriteWindowsSheet(workbook, windows, normalizationFactor);

        // Sheet 2: Session Summary
        WriteSessionSummarySheet(workbook, sessionMetrics);

        // Sheet 3: Global Summary
        WriteGlobalSummarySheet(workbook, globalMetrics);

        workbook.SaveAs(outputPath);
    }

    private void WriteWindowsSheet(XLWorkbook workbook, List<EegWindowRecord> windows, double k)
    {
        var worksheet = workbook.Worksheets.Add("EEG_Windows_With_CME");
        var calculator = new CmeCalculator();

        // Headers
        int col = 1;
        worksheet.Cell(1, col++).Value = "SessionId";
        worksheet.Cell(1, col++).Value = "UserId";
        worksheet.Cell(1, col++).Value = "TaskId";
        worksheet.Cell(1, col++).Value = "StartUtc";
        worksheet.Cell(1, col++).Value = "EndUtc";
        worksheet.Cell(1, col++).Value = "DeltaPower";
        worksheet.Cell(1, col++).Value = "ThetaPower";
        worksheet.Cell(1, col++).Value = "AlphaPower";
        worksheet.Cell(1, col++).Value = "BetaPower";
        worksheet.Cell(1, col++).Value = "ComplexityIndex";
        worksheet.Cell(1, col++).Value = "FlowProbability";
        worksheet.Cell(1, col++).Value = "Eband";
        worksheet.Cell(1, col++).Value = "c(t)";
        worksheet.Cell(1, col++).Value = "p_flow(t)";
        worksheet.Cell(1, col++).Value = "DeltaSeconds";
        worksheet.Cell(1, col++).Value = "CME_raw";
        worksheet.Cell(1, col++).Value = "CME";
        worksheet.Cell(1, col++).Value = "IsFlowWindow";

        // Add extra columns if present
        var extraColumns = windows.SelectMany(w => w.ExtraColumns.Keys).Distinct().ToList();
        foreach (var extraCol in extraColumns)
        {
            worksheet.Cell(1, col++).Value = extraCol;
        }

        // Data rows
        int row = 2;
        foreach (var window in windows)
        {
            col = 1;
            worksheet.Cell(row, col++).Value = window.SessionId;
            worksheet.Cell(row, col++).Value = window.UserId ?? "";
            worksheet.Cell(row, col++).Value = window.TaskId ?? "";
            worksheet.Cell(row, col++).Value = window.StartUtc;
            worksheet.Cell(row, col++).Value = window.EndUtc;
            worksheet.Cell(row, col++).Value = window.DeltaPower;
            worksheet.Cell(row, col++).Value = window.ThetaPower;
            worksheet.Cell(row, col++).Value = window.AlphaPower;
            worksheet.Cell(row, col++).Value = window.BetaPower;
            worksheet.Cell(row, col++).Value = window.ComplexityIndex;
            worksheet.Cell(row, col++).Value = window.FlowProbability;

            double eBand = calculator.CalculateEnergy(window);
            double g = calculator.CalculateModulation(window.ComplexityIndex, window.FlowProbability);
            double cmeRaw = calculator.CalculateRawCme(window);
            double cme = calculator.CalculateCme(window, k);
            bool isFlow = calculator.IsFlowWindow(window);

            worksheet.Cell(row, col++).Value = eBand;
            worksheet.Cell(row, col++).Value = window.ComplexityIndex;
            worksheet.Cell(row, col++).Value = window.FlowProbability;
            worksheet.Cell(row, col++).Value = window.GetDeltaSeconds();
            worksheet.Cell(row, col++).Value = cmeRaw;
            worksheet.Cell(row, col++).Value = cme;
            worksheet.Cell(row, col++).Value = isFlow;

            // Extra columns
            foreach (var extraCol in extraColumns)
            {
                worksheet.Cell(row, col++).Value = window.ExtraColumns.GetValueOrDefault(extraCol)?.ToString() ?? "";
            }

            row++;
        }

        // Format header row
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private void WriteSessionSummarySheet(XLWorkbook workbook, List<SessionMetrics> sessions)
    {
        var worksheet = workbook.Worksheets.Add("Session_Summary");

        // Headers
        int col = 1;
        worksheet.Cell(1, col++).Value = "SessionId";
        worksheet.Cell(1, col++).Value = "UserId";
        worksheet.Cell(1, col++).Value = "TotalWindows";
        worksheet.Cell(1, col++).Value = "TotalDurationSeconds";
        worksheet.Cell(1, col++).Value = "FlowWindows";
        worksheet.Cell(1, col++).Value = "FlowDurationSeconds";
        worksheet.Cell(1, col++).Value = "FlowShare";
        worksheet.Cell(1, col++).Value = "LongestFlowStreakSeconds";
        worksheet.Cell(1, col++).Value = "AvgCME";
        worksheet.Cell(1, col++).Value = "MaxCME";
        worksheet.Cell(1, col++).Value = "CME_session";

        // Data rows
        int row = 2;
        foreach (var session in sessions)
        {
            col = 1;
            worksheet.Cell(row, col++).Value = session.SessionId;
            worksheet.Cell(row, col++).Value = session.UserId ?? "";
            worksheet.Cell(row, col++).Value = session.TotalWindows;
            worksheet.Cell(row, col++).Value = session.TotalDurationSeconds;
            worksheet.Cell(row, col++).Value = session.FlowWindows;
            worksheet.Cell(row, col++).Value = session.FlowDurationSeconds;
            worksheet.Cell(row, col++).Value = session.FlowShare;
            worksheet.Cell(row, col++).Value = session.LongestFlowStreakSeconds;
            worksheet.Cell(row, col++).Value = session.AvgCme;
            worksheet.Cell(row, col++).Value = session.MaxCme;
            worksheet.Cell(row, col++).Value = session.CmeSession;
            row++;
        }

        // Format header row
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private void WriteGlobalSummarySheet(XLWorkbook workbook, GlobalMetrics global)
    {
        var worksheet = workbook.Worksheets.Add("Global_Summary");

        int row = 1;
        worksheet.Cell(row++, 1).Value = "TotalSessions";
        worksheet.Cell(row++, 1).Value = "Mean_CME_session";
        worksheet.Cell(row++, 1).Value = "Median_CME_session";
        worksheet.Cell(row++, 1).Value = "Mean_FlowShare";
        worksheet.Cell(row++, 1).Value = "Sessions_FlowShare_GE_0.5";
        worksheet.Cell(row++, 1).Value = "Sessions_FlowShare_GE_0.7";
        worksheet.Cell(row++, 1).Value = "k";
        worksheet.Cell(row++, 1).Value = "w_delta";
        worksheet.Cell(row++, 1).Value = "w_theta";
        worksheet.Cell(row++, 1).Value = "w_alpha";
        worksheet.Cell(row++, 1).Value = "w_beta";
        worksheet.Cell(row++, 1).Value = "lambda1";
        worksheet.Cell(row++, 1).Value = "lambda2";
        worksheet.Cell(row++, 1).Value = "lambda3";
        worksheet.Cell(row++, 1).Value = "FlowThreshold";

        row = 1;
        worksheet.Cell(row++, 2).Value = global.TotalSessions;
        worksheet.Cell(row++, 2).Value = global.MeanCmeSession;
        worksheet.Cell(row++, 2).Value = global.MedianCmeSession;
        worksheet.Cell(row++, 2).Value = global.MeanFlowShare;
        worksheet.Cell(row++, 2).Value = global.SessionsFlowShareGe05;
        worksheet.Cell(row++, 2).Value = global.SessionsFlowShareGe07;
        worksheet.Cell(row++, 2).Value = global.K;
        worksheet.Cell(row++, 2).Value = global.WDelta;
        worksheet.Cell(row++, 2).Value = global.WTheta;
        worksheet.Cell(row++, 2).Value = global.WAlpha;
        worksheet.Cell(row++, 2).Value = global.WBeta;
        worksheet.Cell(row++, 2).Value = global.Lambda1;
        worksheet.Cell(row++, 2).Value = global.Lambda2;
        worksheet.Cell(row++, 2).Value = global.Lambda3;
        worksheet.Cell(row++, 2).Value = global.FlowThreshold;

        // Format
        worksheet.Column(1).Style.Font.Bold = true;
    }
}


