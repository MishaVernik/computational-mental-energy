using System.Diagnostics;
using MatrixMult.Algorithms;
using MatrixMult.Core;

namespace MatrixMult.Bench;

/// <summary>
/// Runs benchmarks for matrix multiplication algorithms.
/// </summary>
public class BenchmarkRunner
{
    private readonly Matrix _a;
    private readonly Matrix _b;
    private readonly Matrix _reference;
    private readonly int _blockSize;
    private readonly int _maxDegreeOfParallelism;
    private readonly double _errorThreshold;

    public BenchmarkRunner(
        Matrix a, Matrix b, Matrix reference,
        int blockSize, int maxDegreeOfParallelism,
        double errorThreshold = Verification.DefaultThreshold)
    {
        _a = a;
        _b = b;
        _reference = reference;
        _blockSize = blockSize;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _errorThreshold = errorThreshold;
    }

    /// <summary>
    /// Runs a benchmark for the given algorithm.
    /// </summary>
    public BenchmarkStats RunBenchmark(IMatrixMultiplier multiplier, int iterations, int warmupRuns)
    {
        var times = new List<double>();
        double maxError = 0.0;
        bool isCorrect = true;

        // Warmup runs
        for (int i = 0; i < warmupRuns; i++)
        {
            var result = RunSingle(multiplier);
            _ = result; // Discard warmup results
        }

        // Actual benchmark runs
        for (int i = 0; i < iterations; i++)
        {
            var (resultMatrix, timing) = RunSingle(multiplier);
            times.Add(timing.TotalTime.TotalMilliseconds);

            // Verify correctness on first run
            if (i == 0)
            {
                maxError = Verification.GetMaxError(_reference, resultMatrix);
                isCorrect = Verification.Verify(_reference, resultMatrix, _errorThreshold);
            }
        }

        return CalculateStats(times, maxError, isCorrect, iterations);
    }

    private (Matrix Result, MultiplyResult Timing) RunSingle(IMatrixMultiplier multiplier)
    {
        var c = new Matrix(_a.Size);
        c.FillZero();
        
        var result = multiplier.Multiply(_a, _b, c, _blockSize, _maxDegreeOfParallelism);
        
        return (c, result);
    }

    private BenchmarkStats CalculateStats(
        List<double> times, double maxError, bool isCorrect, int iterations)
    {
        times.Sort();

        var stats = new BenchmarkStats
        {
            Iterations = iterations,
            MaxError = maxError,
            IsCorrect = isCorrect
        };

        stats.AvgTimeMs = times.Average();
        stats.MinTimeMs = times.Min();
        stats.MaxTimeMs = times.Max();
        stats.P95TimeMs = GetPercentile(times, 0.95);
        stats.P99TimeMs = GetPercentile(times, 0.99);

        // Calculate throughput: 2*N^3 operations / time
        int n = _a.Size;
        double operations = 2.0 * n * n * n; // 2*N^3 for matrix multiply
        stats.ThroughputGFlops = (operations / (stats.AvgTimeMs / 1000.0)) / 1e9;

        return stats;
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        double index = percentile * (sortedValues.Count - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedValues[lower];

        double weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }

    /// <summary>
    /// Calculates speedup and efficiency relative to sequential baseline.
    /// </summary>
    public void CalculateRelativeStats(
        BenchmarkStats sequential, BenchmarkStats parallel)
    {
        parallel.Speedup = sequential.AvgTimeMs / parallel.AvgTimeMs;
        parallel.Efficiency = parallel.Speedup / _maxDegreeOfParallelism;
    }
}

