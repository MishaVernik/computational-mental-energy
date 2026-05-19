using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using CmeSim.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CmeSim.Api.Controllers;

/// <summary>
/// Controller for processing Mind Monitor (Muse headband) CSV data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MindMonitorController : ControllerBase
{
    private readonly CmeSimDbContext _dbContext;
    private readonly IQuantumBackendClient _quantumClient;
    private readonly ICmeCalculator _cmeCalculator;
    private readonly ILogger<MindMonitorController> _logger;

    public MindMonitorController(
        CmeSimDbContext dbContext,
        IQuantumBackendClient quantumClient,
        ICmeCalculator cmeCalculator,
        ILogger<MindMonitorController> logger)
    {
        _dbContext = dbContext;
        _quantumClient = quantumClient;
        _cmeCalculator = cmeCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Process Mind Monitor CSV data and compute CME for each window.
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<MindMonitorProcessResult>> ProcessMindMonitorData([FromBody] ProcessMindMonitorRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Mind Monitor CSV: {Lines} lines, time filter: {Start} to {End}",
                request.CsvData.Split('\n').Length, request.StartTime, request.EndTime);

            // Parse CSV
            var windows = MindMonitorParser.ParseMindMonitorCsv(
                request.CsvData,
                request.StartTime,
                request.EndTime);

            if (!windows.Any())
            {
                return BadRequest("No valid EEG data found in CSV (check time range and format)");
            }

            _logger.LogInformation("Parsed {Count} valid EEG windows", windows.Count);

            // Create or get session
            var sessionId = Guid.NewGuid();
            var session = new Session
            {
                Id = sessionId,
                UserId = request.UserId ?? "muse_user",
                StartedAt = windows.Min(w => w.Timestamp),
                EndedAt = windows.Max(w => w.Timestamp)
            };
            _dbContext.Sessions.Add(session);
            await _dbContext.SaveChangesAsync();

            // Process each window (limit to prevent timeout)
            var results = new List<MindMonitorWindowResult>();
            var maxWindows = Math.Min(windows.Count, request.MaxWindows ?? 50);

            for (int i = 0; i < maxWindows; i++)
            {
                var window = windows[i];
                
                try
                {
                    // Call quantum backend
                    var quantumResult = await _quantumClient.InferAsync(window.Features);
                    
                    // Compute CME (Вн + index)
                    var cmeCalcResult = _cmeCalculator.ComputeCme(
                        window.Features,
                        quantumResult.PFlow,
                        request.TaskDifficulty ?? 0.5);

                    // Store result
                    var cmeWindowResult = new CmeWindowResult
                    {
                        Id = Guid.NewGuid(),
                        SessionId = sessionId,
                        WindowId = window.WindowId,
                        ComputedAt = DateTime.UtcNow,
                        CmeValue = cmeCalcResult.CmeVn,
                        PFlow = quantumResult.PFlow,
                        ShotsUsed = quantumResult.ShotsUsed,
                        Depth = quantumResult.Depth
                    };
                    _dbContext.CmeWindowResults.Add(cmeWindowResult);

                    results.Add(new MindMonitorWindowResult
                    {
                        Timestamp = window.Timestamp,
                        WindowId = window.WindowId,
                        Cme = cmeCalcResult.CmeVn,
                        PFlow = quantumResult.PFlow,
                        RawBands = window.RawBands
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process window {WindowId}", window.WindowId);
                    // Continue with next window
                }
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Processed {Count} windows successfully", results.Count);

            // Compute summary statistics
            var avgCme = results.Average(r => r.Cme);
            var avgPFlow = results.Average(r => r.PFlow);
            var maxCme = results.Max(r => r.Cme);
            var timeInFlow = results.Count(r => r.PFlow > 0.6) / (double)results.Count;

            return Ok(new MindMonitorProcessResult
            {
                SessionId = sessionId.ToString(),
                TotalWindows = windows.Count,
                ProcessedWindows = results.Count,
                StartTime = windows.Min(w => w.Timestamp),
                EndTime = windows.Max(w => w.Timestamp),
                Results = results,
                Summary = new MindMonitorSummary
                {
                    AvgCme = avgCme,
                    MaxCme = maxCme,
                    AvgPFlow = avgPFlow,
                    TimeInFlowPercentage = timeInFlow * 100,
                    TotalDurationMinutes = (session.EndedAt!.Value - session.StartedAt).TotalMinutes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Mind Monitor data");
            return StatusCode(500, $"Processing failed: {ex.Message}");
        }
    }
}

public class ProcessMindMonitorRequest
{
    public string CsvData { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? UserId { get; set; }
    public double? TaskDifficulty { get; set; }
    public int? MaxWindows { get; set; }
}

public class MindMonitorProcessResult
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalWindows { get; set; }
    public int ProcessedWindows { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<MindMonitorWindowResult> Results { get; set; } = new();
    public MindMonitorSummary Summary { get; set; } = new();
}

public class MindMonitorWindowResult
{
    public DateTime Timestamp { get; set; }
    public string WindowId { get; set; } = string.Empty;
    public double Cme { get; set; }
    public double PFlow { get; set; }
    public Dictionary<string, double>? RawBands { get; set; }
}

public class MindMonitorSummary
{
    public double AvgCme { get; set; }
    public double MaxCme { get; set; }
    public double AvgPFlow { get; set; }
    public double TimeInFlowPercentage { get; set; }
    public double TotalDurationMinutes { get; set; }
}

