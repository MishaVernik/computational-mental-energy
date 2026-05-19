using CmeSim.Api.Data;
using CmeSim.Api.DTOs;
using CmeSim.Api.Models;
using System.Threading.Channels;

namespace CmeSim.Api.Services.Pipelines;

/// <summary>
/// Architecture C: Brokered pipeline - API enqueues to broker, worker nodes process asynchronously.
/// For benchmarking, we wait for the complete end-to-end processing to get comparable metrics.
/// </summary>
public class BrokeredPipeline : IInferencePipeline
{
    private readonly IBrokerQueue _broker;
    private readonly ILogger<BrokeredPipeline> _logger;

    public BrokeredPipeline(
        IBrokerQueue broker,
        ILogger<BrokeredPipeline> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    public async Task<InferenceResponseDto> ExecuteAsync(
        InferenceRequestDto request,
        BenchmarkContext context,
        CancellationToken cancellationToken = default)
    {
        var overallStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.RequestReceived);

        // Stage 1: Validate
        var validateStart = DateTime.UtcNow;
        if (request.Features.Length == 0)
        {
            throw new ArgumentException("Features array cannot be empty");
        }
        if (request.TaskDifficulty < 0 || request.TaskDifficulty > 1)
        {
            throw new ArgumentException("TaskDifficulty must be between 0 and 1");
        }
        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new ArgumentException("Invalid SessionId format");
        }
        var validateMs = (DateTime.UtcNow - validateStart).TotalMilliseconds;
        context.RecordStage("validate", validateMs);
        await context.EmitEventAsync(BenchmarkEventType.Validated, validateMs);

        // Stage 2: Enqueue to broker
        var enqueueStart = DateTime.UtcNow;
        await context.EmitEventAsync(BenchmarkEventType.EnqueuedToWorker);
        
        // Simulate broker delay (message serialization, network to broker)
        if (context.Config.BrokerProfile.MeanMs > 0)
        {
            var brokerDelay = SimulateDelay(
                context.Config.BrokerProfile.MeanMs,
                context.Config.BrokerProfile.StdMs,
                context.Config.Seed,
                context.Config.BrokerProfile.Mode);
            await Task.Delay(TimeSpan.FromMilliseconds(brokerDelay), cancellationToken);
        }
        
        // Create completion source to wait for worker to finish
        var completionSource = new TaskCompletionSource<InferenceResponseDto>();
        
        var workItem = new BrokerWorkItem
        {
            RequestId = context.RequestId,
            BenchmarkRunId = context.BenchmarkRunId,
            Request = request,
            Context = context,
            CompletionSource = completionSource
        };
        
        await _broker.EnqueueAsync(workItem, cancellationToken);
        
        var enqueueMs = (DateTime.UtcNow - enqueueStart).TotalMilliseconds;
        context.RecordStage("enqueue", enqueueMs);
        await context.EmitEventAsync(BenchmarkEventType.AckSent, enqueueMs);

        // Wait for worker to complete processing (end-to-end flow)
        InferenceResponseDto result;
        try
        {
            // Wait for the worker to complete with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(10)); // 10 minute timeout
            
            result = await completionSource.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Brokered request {RequestId} timed out waiting for worker", context.RequestId);
            throw new TimeoutException($"Request {context.RequestId} timed out waiting for worker processing");
        }

        // Record total response time (end-to-end)
        var responseMs = (DateTime.UtcNow - overallStart).TotalMilliseconds;
        context.RecordStage("response", responseMs);
        await context.EmitEventAsync(BenchmarkEventType.ResponseSent, responseMs);

        return result;
    }

    private double SimulateDelay(double meanMs, double stdMs, int? seed, BrokerMode mode)
    {
        if (mode == BrokerMode.Exponential)
        {
            // Exponential distribution
            if (seed.HasValue)
            {
                var random = new Random(seed.Value);
                return -meanMs * Math.Log(1.0 - random.NextDouble());
            }
            else
            {
                var random = new Random();
                return -meanMs * Math.Log(1.0 - random.NextDouble());
            }
        }
        else
        {
            // Normal distribution
            if (seed.HasValue)
            {
                var random = new Random(seed.Value);
                double u1 = random.NextDouble();
                double u2 = random.NextDouble();
                double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                return Math.Max(0, meanMs + stdMs * z);
            }
            else
            {
                var random = new Random();
                double u1 = random.NextDouble();
                double u2 = random.NextDouble();
                double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                return Math.Max(0, meanMs + stdMs * z);
            }
        }
    }
}

/// <summary>
/// Work item for broker queue.
/// </summary>
public class BrokerWorkItem
{
    public Guid RequestId { get; set; }
    public Guid BenchmarkRunId { get; set; }
    public InferenceRequestDto Request { get; set; } = null!;
    public BenchmarkContext Context { get; set; } = null!;
    
    /// <summary>
    /// Completion source for waiting on end-to-end processing.
    /// Worker sets this when processing is complete.
    /// </summary>
    public TaskCompletionSource<InferenceResponseDto>? CompletionSource { get; set; }
}

/// <summary>
/// In-memory broker queue interface (can be replaced with Redis/RabbitMQ later).
/// </summary>
public interface IBrokerQueue
{
    Task EnqueueAsync(BrokerWorkItem item, CancellationToken cancellationToken = default);
    Task<BrokerWorkItem?> DequeueAsync(CancellationToken cancellationToken = default);
    int GetQueueLength();
}

/// <summary>
/// In-memory broker implementation using Channel.
/// </summary>
public class InMemoryBrokerQueue : IBrokerQueue
{
    private readonly Channel<BrokerWorkItem> _channel;
    private readonly ILogger<InMemoryBrokerQueue> _logger;

    public InMemoryBrokerQueue(ILogger<InMemoryBrokerQueue> logger)
    {
        _logger = logger;
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<BrokerWorkItem>(options);
    }

    public async Task EnqueueAsync(BrokerWorkItem item, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public async Task<BrokerWorkItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryRead(out var item))
            {
                return item;
            }
        }
        return null;
    }

    public int GetQueueLength()
    {
        // Approximate queue length
        return _channel.Reader.CanCount ? _channel.Reader.Count : 0;
    }
}
