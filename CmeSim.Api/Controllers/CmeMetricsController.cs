using Cme.Core;
using CmeSim.Api.DTOs;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for processing Excel files and computing CME metrics.
/// Integrated with existing CME inference system.
/// </summary>
[ApiController]
[Route("api/cme")]
public class CmeMetricsController : ControllerBase
{
    private readonly ICmeMetricsService _metricsService;
    private readonly ILogger<CmeMetricsController> _logger;

    public CmeMetricsController(
        ICmeMetricsService metricsService,
        ILogger<CmeMetricsController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Process Excel or CSV file and compute CME metrics.
    /// Returns JSON summary (no per-window data to keep payload small).
    /// </summary>
    [HttpPost("compute-from-excel")]
    public async Task<ActionResult<CmeMetricsResponseDto>> ComputeFromExcel(
        IFormFile file,
        [FromForm] string? worksheetName = null,
        [FromForm] string? configJson = null,
        [FromForm] string? startTime = null,
        [FromForm] string? endTime = null)
    {
        // Check if it's a CSV file
        if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessCsvInternal(file, startTime, endTime, configJson);
        }

        return await ProcessExcelInternal(file, worksheetName, configJson, returnExcel: false);
    }

    private async Task<IActionResult> ProcessCsvForDownload(
        IFormFile file,
        string? configJson)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            _logger.LogInformation("Processing CSV file for download: {FileName}", file.FileName);

            // Parse config if provided
            CmeConfig? config = null;
            if (!string.IsNullOrEmpty(configJson))
            {
                try
                {
                    config = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(configJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse config JSON, using defaults");
                }
            }

            // Read CSV content
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();

            // Process CSV
            var (sessions, global) = await _metricsService.ProcessCsvAsync(csvContent, null, null, config);

            // Read original CSV to get windows
            var csvReader = new Cme.Core.CsvCmeReader();
            var windows = csvReader.ReadCsv(csvContent, null, null);

            // Generate output filename
            var outputFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_cme_results.xlsx";
            var tempOutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

            try
            {
                // Write results to Excel
                var writer = new Cme.Core.ExcelCmeWriter();
                var metricsCalculator = new Cme.Core.MetricsCalculator(config);
                var (_, _, k) = metricsCalculator.CalculateMetrics(windows);
                writer.WriteResults(tempOutputPath, windows, sessions, global, k);

                // Return file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempOutputPath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outputFileName);
            }
            finally
            {
                if (System.IO.File.Exists(tempOutputPath))
                {
                    System.IO.File.Delete(tempOutputPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file for download");
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }

    private async Task<ActionResult<CmeMetricsResponseDto>> ProcessCsvInternal(
        IFormFile file,
        string? startTime,
        string? endTime,
        string? configJson)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            _logger.LogInformation("Processing CSV file: {FileName}, Size: {Size} bytes", 
                file.FileName, file.Length);

            // Parse config if provided
            CmeConfig? config = null;
            if (!string.IsNullOrEmpty(configJson))
            {
                try
                {
                    config = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(configJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse config JSON, using defaults");
                }
            }

            // Parse time range if provided
            DateTime? parsedStartTime = null;
            DateTime? parsedEndTime = null;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out var st))
            {
                parsedStartTime = st;
            }
            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out var et))
            {
                parsedEndTime = et;
            }

            // Read CSV content
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();

            // Process CSV
            var (sessions, global) = await _metricsService.ProcessCsvAsync(csvContent, parsedStartTime, parsedEndTime, config);

            return Ok(new CmeMetricsResponseDto
            {
                GlobalSummary = new GlobalMetricsDto
                {
                    TotalSessions = global.TotalSessions,
                    MeanCmeSession = global.MeanCmeSession,
                    MedianCmeSession = global.MedianCmeSession,
                    MeanFlowShare = global.MeanFlowShare,
                    SessionsFlowShareGe05 = global.SessionsFlowShareGe05,
                    SessionsFlowShareGe07 = global.SessionsFlowShareGe07,
                    K = global.K,
                    WDelta = global.WDelta,
                    WTheta = global.WTheta,
                    WAlpha = global.WAlpha,
                    WBeta = global.WBeta,
                    Lambda1 = global.Lambda1,
                    Lambda2 = global.Lambda2,
                    Lambda3 = global.Lambda3,
                    FlowThreshold = global.FlowThreshold
                },
                SessionSummaries = sessions.Select(s => new SessionMetricsDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    TotalWindows = s.TotalWindows,
                    TotalDurationSeconds = s.TotalDurationSeconds,
                    FlowWindows = s.FlowWindows,
                    FlowDurationSeconds = s.FlowDurationSeconds,
                    FlowShare = s.FlowShare,
                    LongestFlowStreakSeconds = s.LongestFlowStreakSeconds,
                    AvgCme = s.AvgCme,
                    MaxCme = s.MaxCme,
                    CmeSession = s.CmeSession,
                    FlowPeriods = s.FlowPeriods.Select(p => new FlowStatePeriodDto
                    {
                        StartTime = p.StartTime,
                        EndTime = p.EndTime,
                        DurationSeconds = p.DurationSeconds,
                        AvgCme = p.AvgCme,
                        AvgPFlow = p.AvgPFlow
                    }).ToList(),
                    WindowDetails = s.WindowDetails.Select(w => new WindowMetricsDto
                    {
                        Timestamp = w.Timestamp,
                        Cme = w.Cme,
                        PFlow = w.PFlow,
                        IsFlow = w.IsFlow
                    }).ToList()
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file");
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }

    /// <summary>
    /// Process Excel file, compute CME metrics, and return Excel file with results.
    /// </summary>
    [HttpPost("compute-from-excel-download")]
    public async Task<IActionResult> ComputeFromExcelDownload(
        IFormFile file,
        [FromForm] string? worksheetName = null,
        [FromForm] string? configJson = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Check if it's a CSV file
            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessCsvForDownload(file, configJson);
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File must be an Excel file (.xlsx or .xls) or CSV (.csv)");
            }

            _logger.LogInformation("Processing Excel file for download: {FileName}", file.FileName);

            // Parse config if provided
            CmeConfig? config = null;
            if (!string.IsNullOrEmpty(configJson))
            {
                try
                {
                    config = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(configJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse config JSON, using defaults");
                }
            }

            // Generate output filename
            var outputFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_cme_results.xlsx";
            var tempOutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

            try
            {
                // Process and write Excel
                using var stream = file.OpenReadStream();
                await _metricsService.ProcessExcelToFileAsync(stream, tempOutputPath, worksheetName, config);

                // Return file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempOutputPath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outputFileName);
            }
            finally
            {
                if (System.IO.File.Exists(tempOutputPath))
                {
                    System.IO.File.Delete(tempOutputPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file for download");
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }

    private async Task<ActionResult<CmeMetricsResponseDto>> ProcessExcelInternal(
        IFormFile file,
        string? worksheetName,
        string? configJson,
        bool returnExcel)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Accept both Excel and CSV files
            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File must be an Excel file (.xlsx or .xls) or CSV file (.csv)");
            }

            // If CSV, use CSV processing
            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                using var csvReader = new StreamReader(file.OpenReadStream());
                var csvContent = await csvReader.ReadToEndAsync();
                
                // Parse config if provided
                CmeConfig? csvConfig = null;
                if (!string.IsNullOrEmpty(configJson))
                {
                    try
                    {
                        csvConfig = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(configJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse config JSON, using defaults");
                    }
                }

                var (csvSessions, csvGlobal) = await _metricsService.ProcessCsvAsync(csvContent, null, null, csvConfig);

                return Ok(new CmeMetricsResponseDto
                {
                    GlobalSummary = new GlobalMetricsDto
                    {
                        TotalSessions = csvGlobal.TotalSessions,
                        MeanCmeSession = csvGlobal.MeanCmeSession,
                        MedianCmeSession = csvGlobal.MedianCmeSession,
                        MeanFlowShare = csvGlobal.MeanFlowShare,
                        SessionsFlowShareGe05 = csvGlobal.SessionsFlowShareGe05,
                        SessionsFlowShareGe07 = csvGlobal.SessionsFlowShareGe07,
                        K = csvGlobal.K,
                        WDelta = csvGlobal.WDelta,
                        WTheta = csvGlobal.WTheta,
                        WAlpha = csvGlobal.WAlpha,
                        WBeta = csvGlobal.WBeta,
                        Lambda1 = csvGlobal.Lambda1,
                        Lambda2 = csvGlobal.Lambda2,
                        Lambda3 = csvGlobal.Lambda3,
                        FlowThreshold = csvGlobal.FlowThreshold
                    },
                    SessionSummaries = csvSessions.Select(s => new SessionMetricsDto
                    {
                        SessionId = s.SessionId,
                        UserId = s.UserId,
                        TotalWindows = s.TotalWindows,
                        TotalDurationSeconds = s.TotalDurationSeconds,
                        FlowWindows = s.FlowWindows,
                        FlowDurationSeconds = s.FlowDurationSeconds,
                        FlowShare = s.FlowShare,
                        LongestFlowStreakSeconds = s.LongestFlowStreakSeconds,
                        AvgCme = s.AvgCme,
                        MaxCme = s.MaxCme,
                        CmeSession = s.CmeSession,
                        FlowPeriods = s.FlowPeriods.Select(p => new FlowStatePeriodDto
                        {
                            StartTime = p.StartTime,
                            EndTime = p.EndTime,
                            DurationSeconds = p.DurationSeconds,
                            AvgCme = p.AvgCme,
                            AvgPFlow = p.AvgPFlow
                        }).ToList(),
                        WindowDetails = s.WindowDetails.Select(w => new WindowMetricsDto
                        {
                            Timestamp = w.Timestamp,
                            Cme = w.Cme,
                            PFlow = w.PFlow,
                            IsFlow = w.IsFlow
                        }).ToList()
                    }).ToList()
                });
            }

            _logger.LogInformation("Processing Excel file: {FileName}, Size: {Size} bytes", 
                file.FileName, file.Length);

            // Parse config if provided
            CmeConfig? excelConfig = null;
            if (!string.IsNullOrEmpty(configJson))
            {
                try
                {
                    excelConfig = System.Text.Json.JsonSerializer.Deserialize<CmeConfig>(configJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse config JSON, using defaults");
                }
            }

            // Process Excel
            using var excelStream = file.OpenReadStream();
            var (excelSessions, excelGlobal) = await _metricsService.ProcessExcelAsync(excelStream, worksheetName, excelConfig);

            return Ok(new CmeMetricsResponseDto
            {
                GlobalSummary = new GlobalMetricsDto
                {
                    TotalSessions = excelGlobal.TotalSessions,
                    MeanCmeSession = excelGlobal.MeanCmeSession,
                    MedianCmeSession = excelGlobal.MedianCmeSession,
                    MeanFlowShare = excelGlobal.MeanFlowShare,
                    SessionsFlowShareGe05 = excelGlobal.SessionsFlowShareGe05,
                    SessionsFlowShareGe07 = excelGlobal.SessionsFlowShareGe07,
                    K = excelGlobal.K,
                    WDelta = excelGlobal.WDelta,
                    WTheta = excelGlobal.WTheta,
                    WAlpha = excelGlobal.WAlpha,
                    WBeta = excelGlobal.WBeta,
                    Lambda1 = excelGlobal.Lambda1,
                    Lambda2 = excelGlobal.Lambda2,
                    Lambda3 = excelGlobal.Lambda3,
                    FlowThreshold = excelGlobal.FlowThreshold
                },
                SessionSummaries = excelSessions.Select(s => new SessionMetricsDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    TotalWindows = s.TotalWindows,
                    TotalDurationSeconds = s.TotalDurationSeconds,
                    FlowWindows = s.FlowWindows,
                    FlowDurationSeconds = s.FlowDurationSeconds,
                    FlowShare = s.FlowShare,
                    LongestFlowStreakSeconds = s.LongestFlowStreakSeconds,
                    AvgCme = s.AvgCme,
                    MaxCme = s.MaxCme,
                    CmeSession = s.CmeSession,
                    FlowPeriods = s.FlowPeriods.Select(p => new FlowStatePeriodDto
                    {
                        StartTime = p.StartTime,
                        EndTime = p.EndTime,
                        DurationSeconds = p.DurationSeconds,
                        AvgCme = p.AvgCme,
                        AvgPFlow = p.AvgPFlow
                    }).ToList(),
                    WindowDetails = s.WindowDetails.Select(w => new WindowMetricsDto
                    {
                        Timestamp = w.Timestamp,
                        Cme = w.Cme,
                        PFlow = w.PFlow,
                        IsFlow = w.IsFlow
                    }).ToList()
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file");
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }
}

