using CmeSim.Api.Data;
using CmeSim.Api.Models;
using CmeSim.Api.Models.FlowDataset;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Services;

/// <summary>
/// Background worker that processes training jobs.
/// 
/// Simulates a metaheuristic optimization loop (genetic algorithm, particle swarm, etc.)
/// that repeatedly calls the quantum backend to evaluate candidate models.
/// </summary>
public class TrainingWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrainingWorkerService> _logger;
    private readonly IConfiguration _configuration;

    public TrainingWorkerService(
        IServiceProvider serviceProvider,
        ILogger<TrainingWorkerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Training Worker Service started");

        var pollingInterval = TimeSpan.FromSeconds(
            _configuration.GetValue<int>("TrainingWorker:PollingIntervalSeconds", 5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing training jobs");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Training Worker Service stopped");
    }

    private async Task ProcessQueuedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();
        var quantumClient = scope.ServiceProvider.GetRequiredService<IQuantumBackendClient>();

        var maxConcurrent = _configuration.GetValue<int>("TrainingWorker:MaxConcurrentJobs", 2);

        // Count currently running jobs
        var runningCount = await dbContext.TrainingJobs
            .CountAsync(j => j.Status == TrainingJobStatus.Running, cancellationToken);

        if (runningCount >= maxConcurrent)
        {
            _logger.LogDebug("Max concurrent jobs reached ({Count}/{Max})", runningCount, maxConcurrent);
            return;
        }

        // Get next queued job
        var job = await dbContext.TrainingJobs
            .Where(j => j.Status == TrainingJobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
        {
            return; // No jobs to process
        }

        _logger.LogInformation("Starting training job {JobId}", job.Id);

        // Start processing in a background task (don't await)
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessTrainingJobAsync(job.Id, quantumClient, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training job {JobId} failed", job.Id);
                await MarkJobFailedAsync(job.Id, ex.Message);
            }
        }, cancellationToken);
    }

    private async Task ProcessTrainingJobAsync(
        Guid jobId,
        IQuantumBackendClient quantumClient,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var job = await dbContext.TrainingJobs.FindAsync(new object[] { jobId }, cancellationToken);
        if (job == null) return;

        // Mark as running
        job.Status = TrainingJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var generationsPerJob = _configuration.GetValue<int>("TrainingWorker:GenerationsPerJob", 10);
        var candidatesPerGeneration = _configuration.GetValue<int>("TrainingWorker:CandidatesPerGeneration", 5);

        job.TotalGenerations = generationsPerJob;
        double bestFitness = 0.0;
        double[] bestParameters = InitializeParameters(); // Start with random parameters
        int totalQpuCalls = 0;

        _logger.LogInformation("Training job {JobId}: Algorithm={Algorithm}, {Generations} generations, {Candidates} candidates each",
            jobId, job.Algorithm, generationsPerJob, candidatesPerGeneration);

        // Load labeled dataset – stratified sample: balanced flow/non-flow, max 100 total
        const int maxSamplePerClass = 50;
        var flowWindows = await dbContext.EegWindowFeatures
            .Where(w => w.FlowLabel == true)
            .OrderBy(w => Guid.NewGuid())
            .Take(maxSamplePerClass)
            .ToListAsync(cancellationToken);
        var nonFlowWindows = await dbContext.EegWindowFeatures
            .Where(w => w.FlowLabel == false)
            .OrderBy(w => Guid.NewGuid())
            .Take(maxSamplePerClass)
            .ToListAsync(cancellationToken);
        var rawWindows = flowWindows.Concat(nonFlowWindows).ToList();

        var labeledData = rawWindows.Select(w =>
        {
            double avgDelta = (w.Delta_TP9 + w.Delta_AF7 + w.Delta_AF8 + w.Delta_TP10) / 4;
            double avgTheta = (w.Theta_TP9 + w.Theta_AF7 + w.Theta_AF8 + w.Theta_TP10) / 4;
            double avgAlpha = (w.Alpha_TP9 + w.Alpha_AF7 + w.Alpha_AF8 + w.Alpha_TP10) / 4;
            double avgBeta  = (w.Beta_TP9  + w.Beta_AF7  + w.Beta_AF8  + w.Beta_TP10)  / 4;
            double avgGamma = (w.Gamma_TP9 + w.Gamma_AF7 + w.Gamma_AF8 + w.Gamma_TP10) / 4;
            double frontalAsym = w.Alpha_AF8 - w.Alpha_AF7;
            double engagement = avgTheta > 0 ? avgBeta / avgTheta : 0.5;
            return new
            {
                w.Id,
                Features = new[] { avgDelta, avgTheta, avgAlpha, avgBeta, avgGamma, frontalAsym, engagement, w.TaskDifficulty },
                w.FlowLabel
            };
        }).ToList();

        var hasLabeledData = labeledData.Count > 0;
        if (hasLabeledData)
            _logger.LogInformation("Job {JobId}: Using {Count} labeled windows (flow={Flow}, non-flow={NonFlow}) for fitness",
                jobId, labeledData.Count, flowWindows.Count, nonFlowWindows.Count);
        else
            _logger.LogWarning("Job {JobId}: No labeled data in EegWindowFeatures, using random features (fitness = p_flow proxy)", jobId);

        // Real metaheuristic optimization loop
        var population = GenerateInitialPopulation(candidatesPerGeneration);
        
        for (int gen = 0; gen < generationsPerJob; gen++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                job.Status = TrainingJobStatus.Cancelled;
                await dbContext.SaveChangesAsync(CancellationToken.None);
                return;
            }

            _logger.LogDebug("Job {JobId}: Generation {Gen}/{Total}", jobId, gen + 1, generationsPerJob);

            var fitnessScores = new List<double>();
            
            for (int candidate = 0; candidate < candidatesPerGeneration; candidate++)
            {
                if (candidate >= population.Count)
                    population.Add(InitializeParameters());
                
                var candidateParams = population[candidate];
                double fitness;

                if (hasLabeledData)
                {
                    // Batch inference: send all samples for this candidate at once
                    var batch = labeledData.Select(row =>
                        (NormalizeToMinus1Plus1(row.Features), (double[]?)candidateParams)).ToList();
                    var results = await quantumClient.InferBatchAsync(batch, "QSVC");
                    totalQpuCalls += results.Count;

                    int correct = 0;
                    for (int i = 0; i < labeledData.Count && i < results.Count; i++)
                    {
                        bool predFlow = results[i].PFlow >= 0.5;
                        if (predFlow == labeledData[i].FlowLabel!.Value) correct++;
                    }
                    fitness = (double)correct / labeledData.Count;
                }
                else
                {
                    var randomFeatures = Enumerable.Range(0, 8).Select(_ => Random.Shared.NextDouble() * 2.0 - 1.0).ToArray();
                    var result = await quantumClient.InferAsync(randomFeatures, "QSVC", candidateParams);
                    totalQpuCalls++;
                    fitness = result.PFlow + Random.Shared.NextDouble() * 0.05;
                }

                fitnessScores.Add(fitness);
                if (fitness > bestFitness)
                {
                    bestFitness = fitness;
                    bestParameters = (double[])candidateParams.Clone();
                    _logger.LogInformation("Job {JobId}: Gen {Gen}, candidate {Cand}, New best fitness {Fitness:F3}",
                        jobId, gen, candidate, bestFitness);
                }
            }
            
            // Evolve population for next generation (using selected algorithm)
            // Ensure population size is maintained
            population = EvolvePopulation(population, fitnessScores, job.Algorithm, candidatesPerGeneration);

            // Simulate CPU work (genetic operators, population update, etc.)
            await Task.Delay(Random.Shared.Next(50, 100), cancellationToken);

            // Update progress
            job.CompletedGenerations = gen + 1;
            job.BestFitness = bestFitness;
            job.TotalQpuCalls = totalQpuCalls;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Save best parameters found
        job.BestParameters = System.Text.Json.JsonSerializer.Serialize(bestParameters);
        
        // Only promote if this model is better than (or equal to) the current active model
        var currentActive = await dbContext.TrainingJobs
            .Where(j => j.IsActiveModel)
            .FirstOrDefaultAsync(cancellationToken);
        if (currentActive == null || bestFitness >= (currentActive.BestFitness ?? 0))
        {
            await DeactivatePreviousModelsAsync(dbContext);
            job.IsActiveModel = true;
            _logger.LogInformation("Model {JobId} promoted as active (fitness {New:F3} >= previous {Old:F3})",
                jobId, bestFitness, currentActive?.BestFitness ?? 0);
        }
        else
        {
            _logger.LogInformation("Model {JobId} NOT promoted (fitness {New:F3} < current {Old:F3})",
                jobId, bestFitness, currentActive.BestFitness);
        }
        
        // Mark as completed
        job.Status = TrainingJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var duration = (job.CompletedAt.Value - job.StartedAt!.Value).TotalSeconds;
        _logger.LogInformation(
            "Training job {JobId} completed: best_fitness={Fitness:F3}, best_params={Params}, qpu_calls={Calls}, duration={Duration:F1}s",
            jobId, bestFitness, string.Join(",", bestParameters.Select(p => p.ToString("F2"))), totalQpuCalls, duration);
        _logger.LogInformation("Model {JobId} is now ACTIVE for inference", jobId);
    }
    
    private static double[] NormalizeToMinus1Plus1(double[] features)
    {
        var maxAbs = features.Max(Math.Abs);
        if (maxAbs == 0) return features;
        return features.Select(f => f / maxAbs).ToArray();
    }

    private const int TotalParams = 24; // 8 params/layer × 3 layers (2 re-upload + 1 base)

    private static double[] InitializeParameters()
    {
        return Enumerable.Range(0, TotalParams)
            .Select(_ => Random.Shared.NextDouble() * Math.PI * 2)
            .ToArray();
    }
    
    private static List<double[]> GenerateInitialPopulation(int size)
    {
        return Enumerable.Range(0, size)
            .Select(_ => InitializeParameters())
            .ToList();
    }
    
    private static List<double[]> EvolvePopulation(List<double[]> population, List<double> fitnessScores, string algorithm, int targetSize)
    {
        // Sort by fitness (descending)
        var sorted = population
            .Select((p, i) => new { Params = p, Fitness = fitnessScores[i] })
            .OrderByDescending(x => x.Fitness)
            .ToList();
        
        // Keep top 50% (elitism)
        int survivorsCount = targetSize / 2;
        var survivors = sorted.Take(survivorsCount).Select(x => x.Params).ToList();
        
        // Generate new candidates to maintain target population size
        var offspring = new List<double[]>();
        while (offspring.Count < targetSize - survivorsCount)
        {
            // Select two random parents from survivors
            if (survivors.Count == 0)
            {
                // Fallback: generate random if no survivors
                offspring.Add(InitializeParameters());
                continue;
            }
            
            var parent1 = survivors[Random.Shared.Next(survivors.Count)];
            var parent2 = survivors[Random.Shared.Next(survivors.Count)];
            
            var child = new double[TotalParams];
            for (int i = 0; i < TotalParams; i++)
            {
                child[i] = Random.Shared.NextDouble() < 0.5 ? parent1[i] : parent2[i];
            }

            double mutationRate = algorithm switch
            {
                "pso" => 0.05,
                "genetic" => 0.10,
                "aco" => 0.15,
                "simulated_annealing" => 0.20,
                _ => 0.10
            };

            for (int i = 0; i < TotalParams; i++)
            {
                if (Random.Shared.NextDouble() < mutationRate)
                {
                    child[i] += (Random.Shared.NextDouble() - 0.5) * 0.5;
                    child[i] = Math.Max(0, Math.Min(Math.PI * 2, child[i]));
                }
            }
            
            offspring.Add(child);
        }
        
        return survivors.Concat(offspring).ToList();
    }
    
    private static async Task DeactivatePreviousModelsAsync(CmeSimDbContext dbContext)
    {
        var activeModels = await dbContext.TrainingJobs
            .Where(j => j.IsActiveModel)
            .ToListAsync();
        
        foreach (var model in activeModels)
        {
            model.IsActiveModel = false;
        }
        
        await dbContext.SaveChangesAsync();
    }

    private async Task MarkJobFailedAsync(Guid jobId, string errorMessage)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmeSimDbContext>();

        var job = await dbContext.TrainingJobs.FindAsync(jobId);
        if (job != null)
        {
            job.Status = TrainingJobStatus.Failed;
            job.ErrorMessage = errorMessage;
            job.CompletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}


