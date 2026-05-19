using System.Globalization;
using System.Linq;

namespace Cme.Core;

/// <summary>
/// Reads EEG window records from CSV files (Mind Monitor format).
/// </summary>
public class CsvCmeReader
{
    public List<EegWindowRecord> ReadCsv(string csvContent, DateTime? startTime = null, DateTime? endTime = null)
    {
        var records = new List<EegWindowRecord>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return records;

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        
        // Check if it's Mind Monitor format
        bool isMindMonitor = headers.Any(h => h.StartsWith("Delta_") || h.StartsWith("Alpha_") || 
                                             h.Equals("TimeStamp", StringComparison.OrdinalIgnoreCase));
        
        if (isMindMonitor)
        {
            return ReadMindMonitorCsv(csvContent, startTime, endTime);
        }
        
        // Otherwise, try standard CSV format
        return ReadStandardCsv(csvContent, headers);
    }

    private List<EegWindowRecord> ReadMindMonitorCsv(string csvContent, DateTime? startTime, DateTime? endTime)
    {
        var records = new List<EegWindowRecord>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return records;

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        
        // Find column indices
        int timeIdx = Array.FindIndex(headers, h => h.Equals("TimeStamp", StringComparison.OrdinalIgnoreCase));
        int[] deltaIndices = FindColumns(headers, "Delta_");
        int[] thetaIndices = FindColumns(headers, "Theta_");
        int[] alphaIndices = FindColumns(headers, "Alpha_");
        int[] betaIndices = FindColumns(headers, "Beta_");

        if (timeIdx == -1) return records;

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
                        if (startTime.HasValue)
                        {
                            baseDate = startTime.Value.Date;
                            firstValidTimestamp = startTime.Value;
                        }
                        else
                        {
                            // Use a reasonable base date (today at midnight) for relative timestamps
                            baseDate = DateTime.Today;
                            firstValidTimestamp = baseDate.Value;
                        }
                    }
                    // For relative times, add to first valid timestamp
                    // This ensures all timestamps are sequential
                    timestamp = firstValidTimestamp.Add(timeSpan);
                    
                    // Update firstValidTimestamp to be the latest timestamp we've seen
                    // This ensures relative times continue from where we left off
                    if (timestamp > firstValidTimestamp)
                    {
                        firstValidTimestamp = timestamp;
                    }
                }
                else
                {
                    continue;
                }

                // Filter by time range
                if (startTime.HasValue && timestamp < startTime.Value) continue;
                if (endTime.HasValue && timestamp > endTime.Value) continue;

                // Extract band powers
                double delta = AverageValues(values, deltaIndices);
                double theta = AverageValues(values, thetaIndices);
                double alpha = AverageValues(values, alphaIndices);
                double beta = AverageValues(values, betaIndices);

                // Skip empty rows (rows with no EEG data)
                // Also skip rows that are event markers (e.g., "/muse/elements/blink")
                bool hasEegData = delta != 0 || theta != 0 || alpha != 0 || beta != 0;
                bool isEventRow = values.Any(v => v != null && (v.Contains("/muse/") || v.Contains("connected") || v.Contains("disconnected")));
                
                if (!hasEegData || isEventRow) continue;

                records.Add(new EegWindowRecord
                {
                    SessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}",
                    StartUtc = timestamp,
                    EndUtc = timestamp.AddSeconds(5), // Default 5s window
                    DeltaPower = delta,
                    ThetaPower = theta,
                    AlphaPower = alpha,
                    BetaPower = beta,
                    ComplexityIndex = 0.5, // Default
                    FlowProbability = 0.0 // Will be computed later if needed
                });
            }
            catch
            {
                continue;
            }
        }

        return records;
    }

    private List<EegWindowRecord> ReadStandardCsv(string csvContent, string[] headers)
    {
        var records = new List<EegWindowRecord>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return records;

        // Find column indices
        int sessionIdx = Array.FindIndex(headers, h => h.Equals("SessionId", StringComparison.OrdinalIgnoreCase) || 
                                                       h.Equals("session_id", StringComparison.OrdinalIgnoreCase));
        int deltaIdx = Array.FindIndex(headers, h => h.Equals("DeltaPower", StringComparison.OrdinalIgnoreCase) || 
                                                    h.Equals("delta", StringComparison.OrdinalIgnoreCase));
        int thetaIdx = Array.FindIndex(headers, h => h.Equals("ThetaPower", StringComparison.OrdinalIgnoreCase) || 
                                                    h.Equals("theta", StringComparison.OrdinalIgnoreCase));
        int alphaIdx = Array.FindIndex(headers, h => h.Equals("AlphaPower", StringComparison.OrdinalIgnoreCase) || 
                                                    h.Equals("alpha", StringComparison.OrdinalIgnoreCase));
        int betaIdx = Array.FindIndex(headers, h => h.Equals("BetaPower", StringComparison.OrdinalIgnoreCase) || 
                                                   h.Equals("beta", StringComparison.OrdinalIgnoreCase));

        if (deltaIdx == -1 || thetaIdx == -1 || alphaIdx == -1 || betaIdx == -1)
        {
            return records; // Missing required columns
        }

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = lines[i].Split(',').Select(v => v.Trim()).ToArray();
                if (values.Length < Math.Max(deltaIdx, Math.Max(thetaIdx, Math.Max(alphaIdx, betaIdx))) + 1)
                    continue;

                records.Add(new EegWindowRecord
                {
                    SessionId = sessionIdx >= 0 && sessionIdx < values.Length ? values[sessionIdx] : $"session_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DeltaPower = double.TryParse(values[deltaIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0,
                    ThetaPower = double.TryParse(values[thetaIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0,
                    AlphaPower = double.TryParse(values[alphaIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var a) ? a : 0,
                    BetaPower = double.TryParse(values[betaIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var b) ? b : 0,
                    ComplexityIndex = 0.5,
                    FlowProbability = 0.0
                });
            }
            catch
            {
                continue;
            }
        }

        return records;
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

        var cleanTime = timeStr.Split(' ')[0].Trim();
        var parts = cleanTime.Split(':');
        
        if (parts.Length == 2)
        {
            // Format: "HH:mm.ss" or "mm:ss.ss"
            // If first part > 23, treat as minutes, otherwise as hours
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var firstPart) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var secondPart))
            {
                if (firstPart > 23)
                {
                    // Treat as minutes:seconds (e.g., "51:54.6")
                    timeSpan = TimeSpan.FromMinutes(firstPart).Add(TimeSpan.FromSeconds(secondPart));
                }
                else
                {
                    // Treat as hours:minutes (e.g., "08:15.5" = 8 hours 15.5 minutes)
                    timeSpan = TimeSpan.FromHours(firstPart).Add(TimeSpan.FromMinutes(secondPart));
                }
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            // Format: "HH:mm:ss" or "HH:mm:ss.ss"
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var mins) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var secs))
            {
                timeSpan = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(mins)).Add(TimeSpan.FromSeconds(secs));
                return true;
            }
        }
        return false;
    }
}

