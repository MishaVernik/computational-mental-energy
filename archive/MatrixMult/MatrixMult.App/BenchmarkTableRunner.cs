using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MatrixMult.Algorithms;
using MatrixMult.Bench;
using MatrixMult.Core;

namespace MatrixMult.App;

/// <summary>
/// Benchmark runner that generates a table of results for different matrix sizes and thread counts.
/// </summary>
class BenchmarkTableRunner
{
    public static int Run(string[] args)
    {
        var matrixSizes = new List<int> { 500, 1000, 2000, 3000, 5000, 10000 };
        var threadCounts = new List<int>();
        int iterations = 3;
        int warmup = 1;
        string algorithm = "all";
        string outputFile = "benchmark-results.csv";

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sizes" when i + 1 < args.Length:
                    matrixSizes = args[++i].Split(',').Select(int.Parse).ToList();
                    break;
                case "--threads" when i + 1 < args.Length:
                    threadCounts = args[++i].Split(',').Select(int.Parse).ToList();
                    break;
                case "--iterations" when i + 1 < args.Length:
                    iterations = int.Parse(args[++i]);
                    break;
                case "--warmup" when i + 1 < args.Length:
                    warmup = int.Parse(args[++i]);
                    break;
                case "--algo" when i + 1 < args.Length:
                    algorithm = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
            }
        }

        // Default thread counts if not specified
        if (threadCounts.Count == 0)
        {
            int maxThreads = Environment.ProcessorCount;
            threadCounts = new List<int> { 1, 2, 4, 8, 16 };
            int power = 16;
            while (power * 2 <= maxThreads)
            {
                power *= 2;
                threadCounts.Add(power);
            }
            if (!threadCounts.Contains(maxThreads))
            {
                threadCounts.Add(maxThreads);
            }
            threadCounts.Sort();
        }

        Console.WriteLine("=== Matrix Multiplication Benchmark Table ===");
        Console.WriteLine($"Matrix Sizes: {string.Join(", ", matrixSizes)}");
        Console.WriteLine($"Thread Counts: {string.Join(", ", threadCounts)}");
        Console.WriteLine($"Iterations: {iterations}");
        Console.WriteLine($"Algorithm: {algorithm}");
        Console.WriteLine($"Max Threads Available: {Environment.ProcessorCount}");
        Console.WriteLine();

        // Create CSV file
        var csv = new StringBuilder();
        csv.AppendLine("MatrixSize,Threads,Algorithm,AvgTimeMs,MinTimeMs,MaxTimeMs,P95TimeMs,P99TimeMs,ThroughputGFlops,Speedup,Efficiency,Correctness,MaxError");

        int totalRuns = matrixSizes.Count * threadCounts.Count;
        int currentRun = 0;

        BenchmarkStats? sequentialBaseline = null;

        foreach (var size in matrixSizes)
        {
            Console.WriteLine($"\n--- Testing Matrix Size: {size}x{size} ---");

            // Generate matrices once per size
            var a = new Matrix(size);
            var b = new Matrix(size);
            a.FillRandom();
            b.FillRandom();

            // Compute reference result
            var reference = new Matrix(size);
            reference.FillZero();
            var sequentialMultiplier = new SequentialMultiplier();
            sequentialMultiplier.Multiply(a, b, reference, 50, 1);

            sequentialBaseline = null;

            foreach (var threads in threadCounts)
            {
                currentRun++;
                Console.WriteLine($"[{currentRun}/{totalRuns}] Size: {size}x{size}, Threads: {threads}");

                var algorithms = new List<(string Name, IMatrixMultiplier Multiplier)>();
                
                if (algorithm == "all" || algorithm == "sequential")
                {
                    algorithms.Add(("Sequential", new SequentialMultiplier()));
                }
                if (algorithm == "all" || algorithm == "striped")
                {
                    algorithms.Add(("Striped", new StripedMultiplier()));
                }
                if (algorithm == "all" || algorithm == "fox")
                {
                    algorithms.Add(("Fox", new FoxMultiplier()));
                }
                if (algorithm == "all" || algorithm == "cannon")
                {
                    algorithms.Add(("Cannon", new CannonMultiplier()));
                }

                foreach (var (algoName, multiplier) in algorithms)
                {
                    try
                    {
                        var runner = new BenchmarkRunner(a, b, reference, 50, threads);
                        var stats = runner.RunBenchmark(multiplier, iterations, warmup);

                        // Calculate speedup and efficiency relative to sequential baseline
                        if (algoName == "Sequential")
                        {
                            sequentialBaseline = stats;
                            stats.Speedup = 1.0;
                            stats.Efficiency = 1.0 / threads;
                        }
                        else if (sequentialBaseline != null)
                        {
                            runner.CalculateRelativeStats(sequentialBaseline, stats);
                        }

                        // Write to CSV
                        csv.AppendLine($"{size},{threads},{algoName}," +
                            $"{stats.AvgTimeMs:F3},{stats.MinTimeMs:F3},{stats.MaxTimeMs:F3}," +
                            $"{stats.P95TimeMs:F3},{stats.P99TimeMs:F3}," +
                            $"{stats.ThroughputGFlops:F3}," +
                            $"{stats.Speedup:F3},{stats.Efficiency:P2}," +
                            $"{(stats.IsCorrect ? "PASS" : "FAIL")},{stats.MaxError:E2}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error running {algoName}: {ex.Message}");
                        csv.AppendLine($"{size},{threads},{algoName},ERROR,,,,,,,,,");
                    }
                }
            }
        }

        // Write CSV file
        File.WriteAllText(outputFile, csv.ToString());
        Console.WriteLine($"\n✅ Benchmark complete! Results saved to: {outputFile}");
        Console.WriteLine($"\nTotal runs: {currentRun}");
        
        return 0;
    }
}

