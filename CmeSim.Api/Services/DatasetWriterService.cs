using System.Threading.Channels;
using CmeSim.Api.Data;
using CmeSim.Api.Models;
using CmeSim.Api.Models.FlowDataset;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Services;

/// <summary>
/// Background service that writes EEG window features to FlowDataset.EegWindowFeatures
/// asynchronously. Resolves ActionSpikeId when timestamp falls within an action interval.
/// </summary>
public class DatasetWriterService : BackgroundService, IDatasetWriterService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatasetWriterService> _logger;
    private readonly Channel<EegWindowWriteRequest> _channel;
    private const int BatchSize = 10;
    private const int BatchDelayMs = 50;

    public DatasetWriterService(IServiceProvider serviceProvider, ILogger<DatasetWriterService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<EegWindowWriteRequest>(1000);
    }

    public void Enqueue(EegWindowWriteRequest request)
    {
        if (_channel.Writer.TryWrite(request))
            return;
        _logger.LogWarning("Dataset write channel full, dropping window {WindowId}", request.WindowId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DatasetWriterService started");
        var batch = new List<EegWindowWriteRequest>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _channel.Reader.WaitToReadAsync(stoppingToken);
                while (_channel.Reader.TryRead(out var req))
                {
                    batch.Add(req);
                    if (batch.Count >= BatchSize)
                        break;
                }

                if (batch.Count > 0)
                {
                    await WriteBatchAsync(batch, stoppingToken);
                    batch.Clear();
                }

                await Task.Delay(BatchDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DatasetWriterService error");
                batch.Clear();
            }
        }

        if (batch.Count > 0)
            await WriteBatchAsync(batch, CancellationToken.None);
        _logger.LogInformation("DatasetWriterService stopped");
    }

    private async Task WriteBatchAsync(List<EegWindowWriteRequest> batch, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var added = 0;
        foreach (var req in batch)
        {
            try
            {
                var features = ToEegWindowFeatures(req);
                features.ActionSpikeId = req.ActionSpikeId
                    ?? await ResolveActionSpikeIdAsync(db, req.SessionId, req.Timestamp, ct);
                // Bootstrap heuristic label: flow_proxy = (alpha_AF7 + alpha_AF8) / (theta_AF7 + theta_AF8)
                var (label, prob) = ComputeHeuristicFlowLabel(req);
                features.FlowLabel = label;
                features.FlowProbability = prob;
                db.EegWindowFeatures.Add(features);
                added++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add EegWindowFeatures for {WindowId}", req.WindowId);
            }
        }

        if (added == 0)
        {
            _logger.LogWarning("Batch of {Count} windows: all failed to add, nothing to save", batch.Count);
            return;
        }

        try
        {
            await db.SaveChangesAsync(ct);
            _logger.LogDebug("Saved {Count} EegWindowFeatures to cme.EegWindowFeatures", added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save EegWindowFeatures batch ({Added} items)", added);
        }
    }

    private static EegWindowFeatures ToEegWindowFeatures(EegWindowWriteRequest req)
    {
        static void SetChannel(Dictionary<string, ChannelBandPowers> ch, string name,
            out double d, out double t, out double a, out double b, out double g)
        {
            if (ch.TryGetValue(name, out var c))
            {
                d = c.Delta; t = c.Theta; a = c.Alpha; b = c.Beta; g = c.Gamma;
            }
            else
            {
                d = t = a = b = g = 0;
            }
        }

        SetChannel(req.Channels, "TP9", out var d9, out var t9, out var a9, out var b9, out var g9);
        SetChannel(req.Channels, "AF7", out var d7, out var t7, out var a7, out var b7, out var g7);
        SetChannel(req.Channels, "AF8", out var d8, out var t8, out var a8, out var b8, out var g8);
        SetChannel(req.Channels, "TP10", out var d10, out var t10, out var a10, out var b10, out var g10);

        return new EegWindowFeatures
        {
            Id = Guid.NewGuid(),
            SessionId = req.SessionId,
            WindowId = req.WindowId,
            Timestamp = req.Timestamp,
            Delta_TP9 = d9, Theta_TP9 = t9, Alpha_TP9 = a9, Beta_TP9 = b9, Gamma_TP9 = g9,
            Delta_AF7 = d7, Theta_AF7 = t7, Alpha_AF7 = a7, Beta_AF7 = b7, Gamma_AF7 = g7,
            Delta_AF8 = d8, Theta_AF8 = t8, Alpha_AF8 = a8, Beta_AF8 = b8, Gamma_AF8 = g8,
            Delta_TP10 = d10, Theta_TP10 = t10, Alpha_TP10 = a10, Beta_TP10 = b10, Gamma_TP10 = g10,
            TaskDifficulty = req.TaskDifficulty,
            Quality = req.Quality
        };
    }

    /// <summary>
    /// Bootstrap heuristic: flow correlates with frontal alpha/theta ratio (EEG research).
    /// Threshold 1.0: alpha/theta > 1 suggests flow.
    /// </summary>
    private static (bool? FlowLabel, double? FlowProbability) ComputeHeuristicFlowLabel(EegWindowWriteRequest req)
    {
        if (!req.Channels.TryGetValue("AF7", out var af7) || !req.Channels.TryGetValue("AF8", out var af8))
            return (null, null);
        var thetaSum = af7.Theta + af8.Theta;
        var alphaSum = af7.Alpha + af8.Alpha;
        if (thetaSum == 0) return (null, null);
        var ratio = alphaSum / thetaSum;
        var prob = Math.Clamp(ratio / 2.0, 0, 1); // ratio ~1 -> prob 0.5, ratio 2 -> 1
        return (ratio >= 1.0, prob);
    }

    private static async Task<Guid?> ResolveActionSpikeIdAsync(CmeSimDbContext db, Guid sessionId, DateTime timestamp, CancellationToken ct)
    {
        var candidates = await db.ActionSpikes
            .Where(a => a.SessionId == sessionId && a.StartTime <= timestamp && a.EndTime >= timestamp)
            .ToListAsync(ct);
        var spike = candidates.OrderByDescending(a => (a.EndTime - a.StartTime).Ticks).FirstOrDefault();
        return spike?.Id;
    }
}
