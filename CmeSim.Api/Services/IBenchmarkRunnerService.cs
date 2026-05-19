using CmeSim.Api.DTOs;
using CmeSim.Api.Models;

namespace CmeSim.Api.Services;

/// <summary>
/// Service for running benchmark scenarios.
/// </summary>
public interface IBenchmarkRunnerService
{
    Task<Guid> StartBenchmarkAsync(BenchmarkScenarioConfigDto config, CancellationToken cancellationToken = default);
    Task<BenchmarkRunResultDto?> GetBenchmarkResultAsync(Guid runId);
    Task<List<BenchmarkRunResultDto>> GetBenchmarkHistoryAsync(int limit = 50);
    Task<PetriNetParamsDto?> GetPetriNetParamsAsync(Guid runId);
}

