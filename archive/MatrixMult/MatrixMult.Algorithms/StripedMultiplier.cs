using System.Collections.Concurrent;
using MatrixMult.Core;

namespace MatrixMult.Algorithms;

/// <summary>
/// Striped algorithm: row/column block-striped decomposition.
/// Each thread processes a set of row blocks from A and corresponding column blocks from B.
/// </summary>
public class StripedMultiplier : IMatrixMultiplier
{
    public MultiplyResult Multiply(Matrix a, Matrix b, Matrix c, int blockSize, int maxDegreeOfParallelism)
    {
        var startTime = DateTime.UtcNow;
        var computeStart = DateTime.UtcNow;

        int n = a.Size;
        int numBlocks = BlockHelper.GetBlockCount(n, blockSize);
        int numThreads = Math.Max(1, maxDegreeOfParallelism);

        // Distribute row blocks across threads
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = numThreads
        };

        Parallel.For(0, numBlocks, parallelOptions, i =>
        {
            // Each thread processes row block i of A
            for (int j = 0; j < numBlocks; j++)
            {
                for (int k = 0; k < numBlocks; k++)
                {
                    int blockRow = i * blockSize;
                    int blockCol = j * blockSize;
                    int kStart = k * blockSize;
                    int kEnd = Math.Min(kStart + blockSize, n);

                    BlockHelper.MultiplyBlock(a, b, c, blockRow, blockCol, kStart, kEnd, blockSize);
                }
            }
        });

        var computeEnd = DateTime.UtcNow;
        var computeTime = computeEnd - computeStart;
        var totalTime = computeEnd - startTime;

        return new MultiplyResult(
            ComputeTime: computeTime,
            CommunicationOverhead: TimeSpan.Zero,
            TotalTime: totalTime);
    }
}

