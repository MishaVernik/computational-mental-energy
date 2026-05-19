using MatrixMult.Core;

namespace MatrixMult.Algorithms;

/// <summary>
/// Interface for matrix multiplication algorithms.
/// </summary>
public interface IMatrixMultiplier
{
    /// <summary>
    /// Multiplies two matrices: C = A * B
    /// </summary>
    /// <param name="a">Left matrix A</param>
    /// <param name="b">Right matrix B</param>
    /// <param name="c">Result matrix C (must be initialized to zeros)</param>
    /// <param name="blockSize">Block size for blocked multiplication</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of parallel workers</param>
    /// <returns>Timing information (compute time, communication overhead, total time)</returns>
    MultiplyResult Multiply(Matrix a, Matrix b, Matrix c, int blockSize, int maxDegreeOfParallelism);
}

/// <summary>
/// Result of matrix multiplication with timing breakdown.
/// </summary>
public record MultiplyResult(
    TimeSpan ComputeTime,
    TimeSpan CommunicationOverhead,
    TimeSpan TotalTime)
{
    public TimeSpan TotalTime { get; } = TotalTime;
}

