using MatrixMult.Core;

namespace MatrixMult.Algorithms;

/// <summary>
/// Cannon's algorithm for parallel matrix multiplication.
/// Uses a p x p process grid where p = floor(sqrt(threads)).
/// Initial skew phase, then cyclic shifts with local multiply in each phase.
/// </summary>
public class CannonMultiplier : IMatrixMultiplier
{
    public MultiplyResult Multiply(Matrix a, Matrix b, Matrix c, int blockSize, int maxDegreeOfParallelism)
    {
        var startTime = DateTime.UtcNow;
        var computeStart = DateTime.UtcNow;
        var commStart = DateTime.UtcNow;
        var commOverhead = TimeSpan.Zero;

        int n = a.Size;
        int numBlocks = BlockHelper.GetBlockCount(n, blockSize);
        int numThreads = Math.Max(1, maxDegreeOfParallelism);
        
        // Calculate process grid: p x p where p = floor(sqrt(threads))
        int p = (int)Math.Floor(Math.Sqrt(numThreads));
        if (p < 1) p = 1;
        int actualWorkers = p * p;

        // Initial skew phase: shift A blocks left, B blocks up
        commStart = DateTime.UtcNow;
        SimulateSkew(p);
        var commEnd = DateTime.UtcNow;
        commOverhead += commEnd - commStart;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = actualWorkers
        };

        // Cannon algorithm: numBlocks phases (one per k-block)
        // Initial skew aligns blocks, then cyclic shifts in each phase
        for (int phase = 0; phase < numBlocks; phase++)
        {
            // Local multiply: each process computes its assigned blocks
            computeStart = DateTime.UtcNow;
            Parallel.For(0, actualWorkers, parallelOptions, workerId =>
            {
                int gridRow = workerId / p;
                int gridCol = workerId % p;
                
                // Process all blocks assigned to this worker in the grid
                for (int i = gridRow; i < numBlocks; i += p)
                {
                    for (int j = gridCol; j < numBlocks; j += p)
                    {
                        // In Cannon: after initial skew and shifts, A[i, (j+i+phase) mod numBlocks] * B[(i+j+phase) mod numBlocks, j]
                        int kBlockIdx = (j + i + phase) % numBlocks;
                        
                        int blockRow = i * blockSize;
                        int blockCol = j * blockSize;
                        int kStart = kBlockIdx * blockSize;
                        int kEnd = Math.Min(kStart + blockSize, n);

                        BlockHelper.MultiplyBlock(a, b, c, blockRow, blockCol, kStart, kEnd, blockSize);
                    }
                }
            });
            computeStart = DateTime.UtcNow; // Track compute time

            // Cyclic shift: A blocks left, B blocks up
            if (phase < numBlocks - 1) // No shift after last phase
            {
                commStart = DateTime.UtcNow;
                SimulateShift(p);
                commEnd = DateTime.UtcNow;
                commOverhead += commEnd - commStart;
            }
        }

        var computeEnd = DateTime.UtcNow;
        var computeTime = computeEnd - computeStart;
        var totalTime = computeEnd - startTime;

        return new MultiplyResult(
            ComputeTime: computeTime,
            CommunicationOverhead: commOverhead,
            TotalTime: totalTime);
    }

    /// <summary>
    /// Simulates initial skew communication overhead.
    /// </summary>
    private void SimulateSkew(int p)
    {
        // Simulate skew: shift A left by row index, B up by col index
        int steps = p;
        for (int i = 0; i < steps; i++)
        {
            Thread.SpinWait(50 * steps);
        }
    }

    /// <summary>
    /// Simulates cyclic shift communication overhead.
    /// </summary>
    private void SimulateShift(int p)
    {
        // Simulate shift: A left by 1, B up by 1
        int steps = p;
        for (int i = 0; i < steps; i++)
        {
            Thread.SpinWait(50 * steps);
        }
    }
}

