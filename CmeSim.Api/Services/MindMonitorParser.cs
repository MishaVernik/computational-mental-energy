using System.Globalization;

namespace CmeSim.Api.Services;

/// <summary>
/// Parser for Mind Monitor (Muse headband) CSV data.
/// Reference: mind-monitor.com
/// </summary>
public class MindMonitorParser
{
    /// <summary>
    /// The most important columns are: TimeStamp, Delta_TP9, Delta_AF7, Delta_AF8, Delta_TP10, Theta_*, Alpha_*, Beta_*, Gamma_*
    /// Parse Mind Monitor CSV and extract normalized features.
    /// Mind Monitor exports: TimeStamp, Delta_TP9, Delta_AF7, Delta_AF8, Delta_TP10, 
    /// Theta_*, Alpha_*, Beta_*, Gamma_*, RAW_*, Accelerometer, Gyro, HeadBandOn, HSI, Battery
    /// </summary>
    public static List<EegWindow> ParseMindMonitorCsv(string csvContent, DateTime? startTime = null, DateTime? endTime = null)
    {
        var windows = new List<EegWindow>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return windows;

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        
        // Find column indices
        var timeIdx = FindColumn(headers, "TimeStamp");
        
        // Delta, Theta, Alpha, Beta, Gamma for each channel (TP9, AF7, AF8, TP10)
        var deltaIndices = new[] { 
            FindColumn(headers, "Delta_TP9"), 
            FindColumn(headers, "Delta_AF7"), 
            FindColumn(headers, "Delta_AF8"), 
            FindColumn(headers, "Delta_TP10") 
        };
        
        var thetaIndices = new[] { 
            FindColumn(headers, "Theta_TP9"), 
            FindColumn(headers, "Theta_AF7"), 
            FindColumn(headers, "Theta_AF8"), 
            FindColumn(headers, "Theta_TP10") 
        };
        
        var alphaIndices = new[] { 
            FindColumn(headers, "Alpha_TP9"), 
            FindColumn(headers, "Alpha_AF7"), 
            FindColumn(headers, "Alpha_AF8"), 
            FindColumn(headers, "Alpha_TP10") 
        };
        
        var betaIndices = new[] { 
            FindColumn(headers, "Beta_TP9"), 
            FindColumn(headers, "Beta_AF7"), 
            FindColumn(headers, "Beta_AF8"), 
            FindColumn(headers, "Beta_TP10") 
        };
        
        var gammaIndices = new[] { 
            FindColumn(headers, "Gamma_TP9"), 
            FindColumn(headers, "Gamma_AF7"), 
            FindColumn(headers, "Gamma_AF8"), 
            FindColumn(headers, "Gamma_TP10") 
        };

        // Track base date for relative timestamps (HH:mm:ss.f format)
        DateTime? baseDate = null;
        var firstValidTimestamp = DateTime.UtcNow.Date; // Default to today
        
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = lines[i].Split(',');
                
                // Parse timestamp
                if (timeIdx == -1 || timeIdx >= values.Length) continue;
                
                var timeStr = values[timeIdx].Trim();
                if (string.IsNullOrEmpty(timeStr)) continue;
                
                DateTime timestamp;
                
                // Try parsing as full DateTime first
                if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                {
                    // Full datetime parsed successfully
                    if (!baseDate.HasValue)
                    {
                        baseDate = timestamp.Date;
                        firstValidTimestamp = timestamp;
                    }
                }
                    else
                    {
                        // Try parsing as relative time (HH:mm:ss.f or mm:ss.f format)
                        // Mind Monitor exports relative times like "51:54.6" (minutes:seconds.milliseconds)
                        if (TryParseRelativeTime(timeStr, out var timeSpan))
                        {
                            if (!baseDate.HasValue)
                            {
                                // Use startTime if provided, otherwise use today's date
                                if (startTime.HasValue)
                                {
                                    baseDate = startTime.Value.Date;
                                    firstValidTimestamp = startTime.Value;
                                }
                                else
                                {
                                    baseDate = DateTime.UtcNow.Date;
                                    firstValidTimestamp = baseDate.Value;
                                }
                            }
                            timestamp = firstValidTimestamp.Add(timeSpan);
                        }
                        else
                        {
                            // Skip rows with unparseable timestamps
                            continue;
                        }
                    }
                
                // Filter by time range if specified
                if (startTime.HasValue && timestamp < startTime.Value) continue;
                if (endTime.HasValue && timestamp > endTime.Value) continue;
                
                // Extract power band values for each channel and average
                var deltaPower = AverageValues(values, deltaIndices);
                var thetaPower = AverageValues(values, thetaIndices);
                var alphaPower = AverageValues(values, alphaIndices);
                var betaPower = AverageValues(values, betaIndices);
                var gammaPower = AverageValues(values, gammaIndices);
                
                // Skip rows with no valid EEG data (blink events, connection events, etc.)
                // Check if at least one band has non-zero values
                if (deltaPower == 0 && thetaPower == 0 && alphaPower == 0 && betaPower == 0 && gammaPower == 0)
                {
                    continue; // Skip empty rows
                }
                
                // Normalize to [-1, 1] range using log transform and z-score
                var features = NormalizeEegFeatures(deltaPower, thetaPower, alphaPower, betaPower, gammaPower);
                
                windows.Add(new EegWindow
                {
                    Timestamp = timestamp,
                    WindowId = $"muse_{timestamp:yyyyMMdd_HHmmss_fff}",
                    Features = features,
                    RawBands = new Dictionary<string, double>
                    {
                        ["Delta"] = deltaPower,
                        ["Theta"] = thetaPower,
                        ["Alpha"] = alphaPower,
                        ["Beta"] = betaPower,
                        ["Gamma"] = gammaPower
                    }
                });
            }
            catch
            {
                // Skip invalid rows
                continue;
            }
        }
        
        return windows;
    }
    
    private static int FindColumn(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
    
    private static double AverageValues(string[] values, int[] indices)
    {
        var validValues = new List<double>();
        foreach (var idx in indices)
        {
            if (idx >= 0 && idx < values.Length && double.TryParse(values[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                validValues.Add(val);
            }
        }
        return validValues.Any() ? validValues.Average() : 0;
    }
    
    private static double[] NormalizeEegFeatures(double delta, double theta, double alpha, double beta, double gamma)
    {
        // Simple normalization: log transform + min-max scaling to [-1, 1]
        var bands = new[] { delta, theta, alpha, beta, gamma };
        
        // Log transform (Muse outputs are typically 0-100 range)
        var logBands = bands.Select(b => Math.Log10(b + 1)).ToArray();
        
        // Normalize each to [-1, 1] range
        var normalized = new double[8];
        
        // Band powers (normalized)
        normalized[0] = NormalizeTo01(logBands[2]); // Alpha (relaxation)
        normalized[1] = NormalizeTo01(logBands[3]) * 2 - 1; // Beta (active) - map to [-1,1]
        normalized[2] = NormalizeTo01(logBands[1]); // Theta (focus)
        normalized[3] = NormalizeTo01(logBands[0]) * 2 - 1; // Delta (deep) - map to [-1,1]
        
        // Frontal asymmetry (simple ratio)
        normalized[4] = (alpha - beta) / (alpha + beta + 1); // Simplified asymmetry
        
        // Parietal asymmetry (use theta/alpha ratio as proxy)
        normalized[5] = (theta - alpha) / (theta + alpha + 1);
        
        // HRV proxy (use gamma variability as approximation)
        normalized[6] = NormalizeTo01(gamma / 10) * 2 - 1;
        
        // Engagement (beta/alpha ratio)
        normalized[7] = (beta - alpha) / (beta + alpha + 1);
        
        // Clamp all to [-1, 1]
        return normalized.Select(v => Math.Max(-1, Math.Min(1, v))).ToArray();
    }
    
    private static double NormalizeTo01(double value)
    {
        // Simple min-max normalization assuming log values are roughly [0, 2]
        return Math.Max(0, Math.Min(1, value / 2.0));
    }
    
    /// <summary>
    /// Parse relative time format from Mind Monitor CSV (e.g., "51:54.6" = 51 minutes 54.6 seconds).
    /// Also handles "HH:mm:ss.f" format.
    /// </summary>
    private static bool TryParseRelativeTime(string timeStr, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;
        
        // Remove any trailing text (like "/muse/event/connected")
        var cleanTime = timeStr.Split(' ')[0].Trim();
        
        // Try parsing as "mm:ss.f" or "HH:mm:ss.f"
        var parts = cleanTime.Split(':');
        
        if (parts.Length == 2)
        {
            // Format: "mm:ss.f" (minutes:seconds.milliseconds)
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var minutes) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
            {
                timeSpan = TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));
                return true;
            }
        }
        else if (parts.Length == 3)
        {
            // Format: "HH:mm:ss.f" (hours:minutes:seconds.milliseconds)
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

public class EegWindow
{
    public DateTime Timestamp { get; set; }
    public string WindowId { get; set; } = string.Empty;
    public double[] Features { get; set; } = Array.Empty<double>();
    public Dictionary<string, double>? RawBands { get; set; }
}

