namespace MatrixMult.Core;

/// <summary>
/// Utilities for block-based matrix operations.
/// </summary>
public static class BlockHelper
{
    /// <summary>
    /// Performs blocked matrix multiplication: C[blockRow, blockCol] += A[blockRow, k] * B[k, blockCol]
    /// </summary>
    /// <param name="a">Left matrix A</param>
    /// <param name="b">Right matrix B</param>
    /// <param name="c">Result matrix C (accumulated)</param>
    /// <param name="blockRow">Starting row of the block in A and C</param>
    /// <param name="blockCol">Starting column of the block in B and C</param>
    /// <param name="kStart">Starting column of A / row of B</param>
    /// <param name="kEnd">Ending column of A / row of B (exclusive)</param>
    /// <param name="blockSize">Size of the block</param>
    public static void MultiplyBlock(
        Matrix a, Matrix b, Matrix c,
        int blockRow, int blockCol, int kStart, int kEnd, int blockSize)
    {
        int rowEnd = Math.Min(blockRow + blockSize, a.Size);
        int colEnd = Math.Min(blockCol + blockSize, b.Size);

        for (int i = blockRow; i < rowEnd; i++)
        {
            for (int j = blockCol; j < colEnd; j++)
            {
                double sum = c[i, j];
                for (int k = kStart; k < kEnd; k++)
                {
                    sum += a[i, k] * b[k, j];
                }
                c[i, j] = sum;
            }
        }
    }

    /// <summary>
    /// Gets the number of blocks needed to cover a dimension of given size.
    /// </summary>
    public static int GetBlockCount(int size, int blockSize)
    {
        return (size + blockSize - 1) / blockSize;
    }
}

