namespace MatrixMult.Core;

/// <summary>
/// Represents a dense N x N matrix with double-precision floating point values.
/// </summary>
public class Matrix
{
    private readonly double[] _data;
    public int Size { get; }

    public Matrix(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Matrix size must be positive", nameof(size));
        
        Size = size;
        _data = new double[size * size];
    }

    public double this[int row, int col]
    {
        get => _data[row * Size + col];
        set => _data[row * Size + col] = value;
    }

    /// <summary>
    /// Gets a reference to the underlying data array for efficient access.
    /// </summary>
    public double[] Data => _data;

    /// <summary>
    /// Fills the matrix with random values in the range [0, 1).
    /// </summary>
    public void FillRandom(Random? rng = null)
    {
        rng ??= Random.Shared;
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = rng.NextDouble();
        }
    }

    /// <summary>
    /// Fills the matrix with zeros.
    /// </summary>
    public void FillZero()
    {
        Array.Clear(_data);
    }

    /// <summary>
    /// Computes the maximum absolute difference between this matrix and another.
    /// Used for correctness validation.
    /// </summary>
    public double MaxAbsoluteError(Matrix other)
    {
        if (Size != other.Size)
            throw new ArgumentException("Matrices must have the same size", nameof(other));

        double maxError = 0.0;
        for (int i = 0; i < _data.Length; i++)
        {
            double error = Math.Abs(_data[i] - other._data[i]);
            if (error > maxError)
                maxError = error;
        }
        return maxError;
    }

    /// <summary>
    /// Creates a copy of this matrix.
    /// </summary>
    public Matrix Clone()
    {
        var clone = new Matrix(Size);
        Array.Copy(_data, clone._data, _data.Length);
        return clone;
    }
}

