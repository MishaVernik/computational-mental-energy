using System.Text.Json.Serialization;

namespace CmeSim.Api.Models;

/// <summary>
/// Configuration for a benchmark scenario run.
/// </summary>
public class BenchmarkScenarioConfig
{
    public string Name { get; set; } = string.Empty;
    public BenchmarkArchitecture Architecture { get; set; }
    
    // Matrix size (optional, for future extension)
    public int? MatrixSize { get; set; }
    
    // Load generation parameters
    public int ActiveClients { get; set; } = 10;
    public int RequestsPerClient { get; set; } = 30;
    public int? DurationSec { get; set; } // Optional: duration-based instead of fixed count
    public int ThinkTimeMs { get; set; } = 100; // Client pacing between requests
    
    // Worker configuration
    public int WorkersCount { get; set; } = 1;
    public int WorkerNodes { get; set; } = 1; // For distributed architectures
    public int WorkersPerNode { get; set; } = 1;
    
    // QPU configuration
    public int MaxConcurrentQpuCalls { get; set; } = 1;
    public int QpuBackends { get; set; } = 1;
    public int Shots { get; set; } = 256;
    public int CircuitDepth { get; set; } = 4;
    public string? DataFilePath { get; set; } // Optional path to CSV with feature windows
    public int? MaxDatasetRows { get; set; } // Optional cap on rows to load
    
    // Training configuration
    public bool TrainingEnabled { get; set; } = false;
    public double TrainingRatePerMin { get; set; } = 0.0;
    
    // Retry policy
    public int MaxRetries { get; set; } = 3;
    public int BackoffMs { get; set; } = 100;
    
    // Network/Database/Broker profiles (simulated delays)
    public NetworkProfile NetworkProfile { get; set; } = new();
    public DatabaseProfile DbProfile { get; set; } = new();
    public BrokerProfile BrokerProfile { get; set; } = new();
    
    // Deterministic seed for reproducibility
    public int? Seed { get; set; }
}

public class NetworkProfile
{
    public double MeanMs { get; set; } = 5.0;
    public double StdMs { get; set; } = 2.0;
}

public class DatabaseProfile
{
    public double MeanMs { get; set; } = 10.0;
    public double StdMs { get; set; } = 3.0;
}

public class BrokerProfile
{
    public double MeanMs { get; set; } = 2.0;
    public double StdMs { get; set; } = 1.0;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BrokerMode Mode { get; set; } = BrokerMode.Exponential;
}

public enum BrokerMode
{
    Exponential,
    Normal
}

