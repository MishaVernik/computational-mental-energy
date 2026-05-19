using Cme.Core;
using CmeSim.Api.Data;
using CmeSim.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Services;

/// <summary>
/// Service for processing Excel files and computing CME metrics.
/// Integrated with existing CME inference system.
/// </summary>
public interface ICmeMetricsService
{
    /// <summary>
    /// Process Excel file and compute CME metrics.
    /// </summary>
    Task<(List<SessionMetrics> Sessions, GlobalMetrics Global)> ProcessExcelAsync(
        Stream excelStream, 
        string? worksheetName = null, 
        CmeConfig? config = null);

    /// <summary>
    /// Process CSV file and compute CME metrics.
    /// </summary>
    Task<(List<SessionMetrics> Sessions, GlobalMetrics Global)> ProcessCsvAsync(
        string csvContent,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CmeConfig? config = null);

    /// <summary>
    /// Process Excel file and write results to output Excel file.
    /// </summary>
    Task ProcessExcelToFileAsync(
        Stream inputStream,
        string outputPath,
        string? worksheetName = null,
        CmeConfig? config = null);
}

public class CmeMetricsService : ICmeMetricsService
{
    private readonly ILogger<CmeMetricsService> _logger;
    private readonly IQuantumBackendClient _quantumClient;
    private readonly ICmeCalculator _cmeCalculator;
    private readonly CmeSimDbContext _dbContext;

    public CmeMetricsService(
        ILogger<CmeMetricsService> logger,
        IQuantumBackendClient quantumClient,
        ICmeCalculator cmeCalculator,
        CmeSimDbContext dbContext)
    {
        _logger = logger;
        _quantumClient = quantumClient;
        _cmeCalculator = cmeCalculator;
        _dbContext = dbContext;
    }

    public async Task<(List<SessionMetrics> Sessions, GlobalMetrics Global)> ProcessExcelAsync(
        Stream excelStream, 
        string? worksheetName = null, 
        CmeConfig? config = null)
    {
        // Excel reading is not currently implemented - use CSV format instead
        throw new NotImplementedException("Excel reading is not currently implemented. Please use CSV format.");
    }

    private async Task<List<EegWindowRecord>> ProcessWindowsWithQuantumBackend(
        List<EegWindowRecord> windows, 
        CmeConfig? config)
    {
        // Get active trained model parameters
        var activeModel = await _dbContext.TrainingJobs
            .Where(j => j.IsActiveModel && !string.IsNullOrEmpty(j.BestParameters) && j.Status == TrainingJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync();

        double[]? trainedParams = null;
        if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
        {
            try
            {
                trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters);
                _logger.LogInformation("Using trained parameters from model {ModelId}, algorithm: {Algorithm}", 
                    activeModel.Id, activeModel.Algorithm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize trained parameters, using defaults");
            }
        }
        else
        {
            _logger.LogInformation("No trained model available, using default quantum parameters");
        }

        // Process each window with quantum backend
        _logger.LogInformation("Processing {Count} windows with quantum backend", windows.Count);
        var processedWindows = new List<EegWindowRecord>();
        
        foreach (var window in windows)
        {
            try
            {
                // Extract features (8 normalized features for quantum backend)
                var features = ExtractFeaturesFromWindow(window);

                // Call quantum backend with trained parameters
                var quantumResult = await _quantumClient.InferAsync(
                    features,
                    "QSVC",
                    trainedParams);

                // Update window with quantum results
                window.FlowProbability = quantumResult.PFlow;
                
                // Note: CME will be computed later in MetricsCalculator using the quantum p_flow
                processedWindows.Add(window);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process window {WindowId}, skipping", window.SessionId);
                // Continue with next window
            }
        }

        _logger.LogInformation("Successfully processed {Count} windows with quantum backend", processedWindows.Count);
        return processedWindows;
    }

    private double[] ExtractFeaturesFromWindow(EegWindowRecord window)
    {
        // Normalize to 8 features: [delta, theta, alpha, beta, frontal_asym, parietal_asym, hrv_proxy, engagement]
        var delta = window.DeltaPower;
        var theta = window.ThetaPower;
        var alpha = window.AlphaPower;
        var beta = window.BetaPower;

        // Log transform and normalize to [-1, 1]
        var logDelta = Math.Log(Math.Max(delta, 0.001));
        var logTheta = Math.Log(Math.Max(theta, 0.001));
        var logAlpha = Math.Log(Math.Max(alpha, 0.001));
        var logBeta = Math.Log(Math.Max(beta, 0.001));

        // Normalize to [0, 1] then map to [-1, 1]
        var normalizedDelta = (logDelta / 2.0) * 2 - 1;
        var normalizedTheta = (logTheta / 2.0) * 2 - 1;
        var normalizedAlpha = (logAlpha / 2.0) * 2 - 1;
        var normalizedBeta = (logBeta / 2.0) * 2 - 1;

        // Frontal asymmetry
        var frontalAsym = (alpha - beta) / (alpha + beta + 1);
        
        // Parietal asymmetry
        var parietalAsym = (theta - alpha) / (theta + alpha + 1);
        
        // HRV proxy
        var hrvProxy = normalizedBeta * 0.5;
        
        // Engagement
        var engagement = (beta - alpha) / (beta + alpha + 1);

        return new[]
        {
            Math.Max(-1, Math.Min(1, normalizedDelta)),
            Math.Max(-1, Math.Min(1, normalizedTheta)),
            Math.Max(-1, Math.Min(1, normalizedAlpha)),
            Math.Max(-1, Math.Min(1, normalizedBeta)),
            Math.Max(-1, Math.Min(1, frontalAsym)),
            Math.Max(-1, Math.Min(1, parietalAsym)),
            Math.Max(-1, Math.Min(1, hrvProxy)),
            Math.Max(-1, Math.Min(1, engagement))
        };
    }

    public async Task<(List<SessionMetrics> Sessions, GlobalMetrics Global)> ProcessCsvAsync(
        string csvContent,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CmeConfig? config = null)
    {
        // Read CSV
        var reader = new CsvCmeReader();
        var windows = reader.ReadCsv(csvContent, startTime, endTime);
        _logger.LogInformation("Read {Count} windows from CSV", windows.Count);

        if (windows.Count == 0)
        {
            throw new InvalidOperationException("No valid EEG windows found in CSV");
        }

        _logger.LogInformation("Starting quantum backend processing for {Count} windows", windows.Count);

        // Get active trained model parameters
        double[]? trainedParams = null;
        try
        {
            var activeModel = await _dbContext.TrainingJobs
                .Where(j => j.IsActiveModel && !string.IsNullOrEmpty(j.BestParameters) && j.Status == TrainingJobStatus.Completed)
                .OrderByDescending(j => j.CompletedAt)
                .FirstOrDefaultAsync();

            if (activeModel != null && !string.IsNullOrEmpty(activeModel.BestParameters))
            {
                try
                {
                    trainedParams = System.Text.Json.JsonSerializer.Deserialize<double[]>(activeModel.BestParameters);
                    _logger.LogInformation("Using trained parameters from model {ModelId}, algorithm: {Algorithm}", 
                        activeModel.Id, activeModel.Algorithm);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize trained parameters, using defaults");
                }
            }
            else
            {
                _logger.LogInformation("No trained model found, using default quantum parameters");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trained model, continuing with defaults");
        }

        // Process each window with quantum backend
        _logger.LogInformation("Processing {Count} windows with quantum backend", windows.Count);
        var processedWindows = new List<EegWindowRecord>();
        int processedCount = 0;
        
        foreach (var window in windows)
        {
            try
            {
                processedCount++;
                
                // Log progress every 10 windows or at start/end
                if (processedCount == 1 || processedCount % 10 == 0 || processedCount == windows.Count)
                {
                    _logger.LogInformation("Processing window {Current}/{Total} with quantum backend", 
                        processedCount, windows.Count);
                }

                // Extract features (8 normalized features for quantum backend)
                var features = ExtractFeaturesFromWindow(window);

                // Call quantum backend with trained parameters
                var quantumResult = await _quantumClient.InferAsync(
                    features,
                    "QSVC",
                    trainedParams);

                // Update window with quantum results
                window.FlowProbability = quantumResult.PFlow;
                
                processedWindows.Add(window);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process window {WindowId} ({Current}/{Total}), skipping", 
                    window.SessionId, processedCount, windows.Count);
                // Continue with next window
            }
        }

        _logger.LogInformation("Successfully processed {Count}/{Total} windows with quantum backend", 
            processedWindows.Count, windows.Count);

        // Calculate metrics with quantum-computed p_flow
        var metricsCalculator = new MetricsCalculator(config);
        var (sessions, global, _) = metricsCalculator.CalculateMetrics(processedWindows);

        return (sessions, global);
    }


    public async Task ProcessExcelToFileAsync(
        Stream inputStream,
        string outputPath,
        string? worksheetName = null,
        CmeConfig? config = null)
    {
        // Excel reading is not currently implemented - use CSV format instead
        throw new NotImplementedException("Excel reading is not currently implemented. Please use CSV format.");
        
        /* TODO: Re-implement ExcelCmeReader and ExcelCmeWriter
        // Save stream to temp file
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempPath))
            {
                await inputStream.CopyToAsync(fileStream);
            }

            var reader = new ExcelCmeReader();
            var windows = reader.ReadExcel(tempPath, worksheetName);
            _logger.LogInformation("Read {Count} windows from Excel", windows.Count);

            // Calculate metrics
            var metricsCalculator = new MetricsCalculator(config);
            var (sessions, global, k) = metricsCalculator.CalculateMetrics(windows);

            // Write results
            var writer = new ExcelCmeWriter();
            writer.WriteResults(outputPath, windows, sessions, global, k);
            _logger.LogInformation("Wrote results to: {OutputPath}", outputPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        */
    }
}

