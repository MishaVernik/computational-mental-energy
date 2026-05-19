namespace CmeSim.Api.Models;

/// <summary>
/// Architecture type for benchmark runs.
/// </summary>
public enum BenchmarkArchitecture
{
    /// <summary>
    /// Architecture A: Monolith - API performs all operations synchronously.
    /// </summary>
    A_Monolith = 0,

    /// <summary>
    /// Architecture B: Synchronous Microservices - API calls PreprocessService via HTTP, then QPU, then DB.
    /// </summary>
    B_SyncMicroservices = 1,

    /// <summary>
    /// Architecture C: Brokered - API enqueues to broker, worker nodes process asynchronously.
    /// </summary>
    C_Brokered = 2
}

