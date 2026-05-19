using ClosedXML.Excel;
using System.Globalization;

namespace CsvToExcelConverter;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: CsvToExcelConverter <input.csv> <output.xlsx> [startTime] [endTime] [taskDifficulty]");
            Console.WriteLine("Example: CsvToExcelConverter data.csv output.xlsx 19:55 21:20 0.7");
            return 1;
        }

        string csvPath = args[0];
        string excelPath = args[1];
        string? startTime = args.Length > 2 ? args[2] : null;
        string? endTime = args.Length > 3 ? args[3] : null;
        double taskDifficulty = args.Length > 4 ? double.Parse(args[4]) : 0.5;

        try
        {
            Console.WriteLine($"Reading CSV: {csvPath}");
            var lines = File.ReadAllLines(csvPath);
            
            if (lines.Length < 2)
            {
                Console.WriteLine("CSV file must have at least a header and one data row");
                return 1;
            }

            var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            
            // Find column indices
            int timeIdx = Array.FindIndex(headers, h => h.Equals("TimeStamp", StringComparison.OrdinalIgnoreCase));
            int[] deltaIndices = FindColumns(headers, "Delta_");
            int[] thetaIndices = FindColumns(headers, "Theta_");
            int[] alphaIndices = FindColumns(headers, "Alpha_");
            int[] betaIndices = FindColumns(headers, "Beta_");

            if (timeIdx == -1)
            {
                Console.WriteLine("TimeStamp column not found!");
                return 1;
            }

            // Parse time range if provided
            DateTime? start = null;
            DateTime? end = null;
            if (!string.IsNullOrWhiteSpace(startTime) && TimeSpan.TryParse(startTime, out var tsStart))
            {
                start = DateTime.Today.Add(tsStart);
            }
            if (!string.IsNullOrWhiteSpace(endTime) && TimeSpan.TryParse(endTime, out var tsEnd))
            {
                end = DateTime.Today.Add(tsEnd);
            }

            Console.WriteLine("Parsing CSV and extracting EEG windows...");
            var windows = new List<(DateTime timestamp, double delta, double theta, double alpha, double beta)>();
            DateTime? baseDate = null;
            DateTime firstValidTimestamp = DateTime.Today;

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = lines[i].Split(',');
                    if (timeIdx >= values.Length) continue;

                    var timeStr = values[timeIdx].Trim();
                    if (string.IsNullOrEmpty(timeStr)) continue;

                    DateTime timestamp;
                    if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                    {
                        if (!baseDate.HasValue)
                        {
                            baseDate = timestamp.Date;
                            firstValidTimestamp = timestamp;
                        }
                    }
                    else if (TryParseRelativeTime(timeStr, out var timeSpan))
                    {
                        if (!baseDate.HasValue)
                        {
                            // Use start time if provided, otherwise use today
                            baseDate = start?.Date ?? DateTime.Today;
                            firstValidTimestamp = start ?? baseDate.Value;
                        }
                        timestamp = firstValidTimestamp.Add(timeSpan);
                    }
                    else
                    {
                        // Try to parse as just time (HH:mm:ss)
                        if (TimeSpan.TryParse(timeStr, out var ts))
                        {
                            if (!baseDate.HasValue)
                            {
                                baseDate = start?.Date ?? DateTime.Today;
                                firstValidTimestamp = start ?? baseDate.Value;
                            }
                            timestamp = firstValidTimestamp.Date.Add(ts);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // Filter by time range
                    if (start.HasValue && timestamp < start.Value) continue;
                    if (end.HasValue && timestamp > end.Value) continue;

                    // Extract band powers
                    double delta = AverageValues(values, deltaIndices);
                    double theta = AverageValues(values, thetaIndices);
                    double alpha = AverageValues(values, alphaIndices);
                    double beta = AverageValues(values, betaIndices);

                    // Skip empty rows
                    if (delta == 0 && theta == 0 && alpha == 0 && beta == 0) continue;

                    windows.Add((timestamp, delta, theta, alpha, beta));
                }
                catch
                {
                    continue;
                }
            }

            Console.WriteLine($"Parsed {windows.Count} EEG windows");

            if (windows.Count == 0)
            {
                Console.WriteLine("No valid EEG windows found!");
                return 1;
            }

            // Convert to Excel format
            Console.WriteLine("Converting to Excel format...");
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("EEG_Windows");

            // Headers
            int col = 1;
            worksheet.Cell(1, col++).Value = "SessionId";
            worksheet.Cell(1, col++).Value = "StartUtc";
            worksheet.Cell(1, col++).Value = "EndUtc";
            worksheet.Cell(1, col++).Value = "DeltaPower";
            worksheet.Cell(1, col++).Value = "ThetaPower";
            worksheet.Cell(1, col++).Value = "AlphaPower";
            worksheet.Cell(1, col++).Value = "BetaPower";
            worksheet.Cell(1, col++).Value = "ComplexityIndex";
            worksheet.Cell(1, col++).Value = "FlowProbability";

            // Format header row
            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data rows
            int row = 2;
            string sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";
            foreach (var (timestamp, delta, theta, alpha, beta) in windows)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = sessionId;
                worksheet.Cell(row, col++).Value = timestamp;
                worksheet.Cell(row, col++).Value = timestamp.AddSeconds(5);
                worksheet.Cell(row, col++).Value = delta;
                worksheet.Cell(row, col++).Value = theta;
                worksheet.Cell(row, col++).Value = alpha;
                worksheet.Cell(row, col++).Value = beta;
                worksheet.Cell(row, col++).Value = taskDifficulty;
                worksheet.Cell(row, col++).Value = 0.0;
                row++;
            }

            Console.WriteLine($"Writing Excel file: {excelPath}");
            workbook.SaveAs(excelPath);
            Console.WriteLine($"✓ Successfully converted {windows.Count} windows to Excel");
            Console.WriteLine($"  Output: {excelPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static int[] FindColumns(string[] headers, string prefix)
    {
        var indices = new List<int>();
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                indices.Add(i);
            }
        }
        return indices.ToArray();
    }

    private static double AverageValues(string[] values, int[] indices)
    {
        var validValues = new List<double>();
        foreach (var idx in indices)
        {
            if (idx >= 0 && idx < values.Length && 
                double.TryParse(values[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                validValues.Add(val);
            }
        }
        return validValues.Any() ? validValues.Average() : 0;
    }

    private static bool TryParseRelativeTime(string timeStr, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(timeStr)) return false;

        // Handle format like "08:15.5" (mm:ss.f) - Mind Monitor format
        var parts = timeStr.Split(':');
        if (parts.Length == 2)
        {
            // Format: "mm:ss.f" (minutes:seconds.milliseconds)
            if (int.TryParse(parts[0], out var mins) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var secs))
            {
                timeSpan = TimeSpan.FromMinutes(mins).Add(TimeSpan.FromSeconds(secs));
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            // Format: "HH:mm:ss.f"
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var mins) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var secs))
            {
                timeSpan = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(mins)).Add(TimeSpan.FromSeconds(secs));
                return true;
            }
        }
        // Also try parsing as TimeSpan directly (handles "HH:mm:ss" format)
        else if (TimeSpan.TryParse(timeStr, out timeSpan))
        {
            return true;
        }
        return false;
    }
}
