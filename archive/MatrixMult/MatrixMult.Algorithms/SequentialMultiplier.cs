using MatrixMult.Core;

namespace MatrixMult.Algorithms;

/// <summary>
/// Sequential baseline matrix multiplication using blocked algorithm.
/// </summary>
public class SequentialMultiplier : IMatrixMultiplier
{
    public MultiplyResult Multiply(Matrix a, Matrix b, Matrix c, int blockSize, int maxDegreeOfParallelism)
    {
        var startTime = DateTime.UtcNow;
        
        int n = a.Size;
        int numBlocks = BlockHelper.GetBlockCount(n, blockSize);

        // Blocked matrix multiplication: C += A * B
        for (int i = 0; i < numBlocks; i++)
        {
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
        }

        var endTime = DateTime.UtcNow;
        var totalTime = endTime - startTime;

        return new MultiplyResult(
            ComputeTime: totalTime,
            CommunicationOverhead: TimeSpan.Zero,
            TotalTime: totalTime);
    }
}

