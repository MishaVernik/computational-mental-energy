using CmeSim.Api.DTOs;
using CmeSim.Api.Models;

namespace CmeSim.Api.Services;

/// <summary>
/// Interface for inference pipeline execution with different architectures.
/// </summary>
public interface IInferencePipeline
{
    /// <summary>
    /// Execute inference request through the pipeline.
    /// </summary>
    Task<InferenceResponseDto> ExecuteAsync(
        InferenceRequestDto request,
        BenchmarkContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for benchmark execution (provides timing, metrics collection, etc.).
/// </summary>
public class BenchmarkContext
{
    public Guid BenchmarkRunId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public BenchmarkScenarioConfig Config { get; set; } = null!;
    public Dictionary<string, DateTime> StageTimestamps { get; set; } = new();
    public Dictionary<string, double> StageDurations { get; set; } = new();
    public Func<BenchmarkEvent, Task>? OnEvent { get; set; }
    
    public void RecordStage(string stageName, double durationMs)
    {
        StageDurations[stageName] = durationMs;
    }
    
    public void RecordTimestamp(string stageName)
    {
        StageTimestamps[stageName] = DateTime.UtcNow;
    }
    
    public async Task EmitEventAsync(BenchmarkEventType eventType, double? durationMs = null, string? metadata = null)
    {
        if (OnEvent != null)
        {
            await OnEvent(new BenchmarkEvent
            {
                BenchmarkRunId = BenchmarkRunId,
                RequestId = RequestId,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                DurationMs = durationMs ?? 0,
                Metadata = metadata
            });
        }
    }
}

