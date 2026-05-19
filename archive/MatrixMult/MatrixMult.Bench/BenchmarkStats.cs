namespace MatrixMult.Bench;

/// <summary>
/// Statistical metrics for benchmark results.
/// </summary>
public class BenchmarkStats
{
    public double AvgTimeMs { get; set; }
    public double MinTimeMs { get; set; }
    public double MaxTimeMs { get; set; }
    public double P95TimeMs { get; set; }
    public double P99TimeMs { get; set; }
    public double ThroughputGFlops { get; set; }
    public double Speedup { get; set; }
    public double Efficiency { get; set; }
    public int Iterations { get; set; }
    public double MaxError { get; set; }
    public bool IsCorrect { get; set; }

    public void Print()
    {
        Console.WriteLine($"  Avg Time:     {AvgTimeMs:F3} ms");
        Console.WriteLine($"  Min Time:     {MinTimeMs:F3} ms");
        Console.WriteLine($"  Max Time:     {MaxTimeMs:F3} ms");
        Console.WriteLine($"  P95 Time:     {P95TimeMs:F3} ms");
        Console.WriteLine($"  P99 Time:     {P99TimeMs:F3} ms");
        Console.WriteLine($"  Throughput:   {ThroughputGFlops:F3} GFlops");
        Console.WriteLine($"  Speedup:      {Speedup:F3}x");
        Console.WriteLine($"  Efficiency:   {Efficiency:P2}");
        Console.WriteLine($"  Correctness:  {(IsCorrect ? "PASS" : "FAIL")} (max error: {MaxError:E2})");
    }
}

