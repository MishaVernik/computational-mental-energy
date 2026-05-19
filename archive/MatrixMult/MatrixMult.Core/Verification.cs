namespace MatrixMult.Core;

/// <summary>
/// Utilities for verifying correctness of matrix multiplication results.
/// </summary>
public static class Verification
{
    /// <summary>
    /// Default threshold for floating-point comparison (1e-10).
    /// </summary>
    public const double DefaultThreshold = 1e-10;

    /// <summary>
    /// Verifies that two matrices are approximately equal within the given threshold.
    /// </summary>
    public static bool Verify(Matrix actual, Matrix expected, double threshold = DefaultThreshold)
    {
        double maxError = actual.MaxAbsoluteError(expected);
        return maxError <= threshold;
    }

    /// <summary>
    /// Gets the maximum absolute error between two matrices.
    /// </summary>
    public static double GetMaxError(Matrix actual, Matrix expected)
    {
        return actual.MaxAbsoluteError(expected);
    }
}

