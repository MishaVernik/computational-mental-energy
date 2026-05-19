using Cme.Core;
using Xunit;

namespace Cme.Core.Tests;

public class CmeCalculatorTests
{
    [Fact]
    public void CalculateEnergy_WithConstantValues_ReturnsExpected()
    {
        // Arrange
        var config = new CmeConfig();
        var calculator = new CmeCalculator(config);
        var window = new EegWindowRecord
        {
            DeltaPower = 1.0,
            ThetaPower = 2.0,
            AlphaPower = 3.0,
            BetaPower = 4.0
        };

        // Act
        double energy = calculator.CalculateEnergy(window);

        // Assert
        // Expected: 0.5*1 + 1.0*2 + 1.0*3 + 0.3*4 = 0.5 + 2 + 3 + 1.2 = 6.7
        Assert.Equal(6.7, energy, 2);
    }

    [Fact]
    public void CalculateModulation_WithConstantValues_ReturnsExpected()
    {
        // Arrange
        var calculator = new CmeCalculator();
        double c = 0.5;
        double p = 0.6;

        // Act
        double g = calculator.CalculateModulation(c, p);

        // Assert
        // Expected: 0.5*0.5 + 0.5*0.6 + 0.5*0.5*0.6 = 0.25 + 0.3 + 0.15 = 0.7
        Assert.Equal(0.7, g, 2);
    }

    [Fact]
    public void CalculateRawCme_WithConstantValues_ReturnsExpected()
    {
        // Arrange
        var calculator = new CmeCalculator();
        var window = new EegWindowRecord
        {
            DeltaPower = 1.0,
            ThetaPower = 2.0,
            AlphaPower = 3.0,
            BetaPower = 4.0,
            ComplexityIndex = 0.5,
            FlowProbability = 0.6,
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddSeconds(5.0)
        };

        // Act
        double rawCme = calculator.CalculateRawCme(window);

        // Assert
        // Energy = 6.7, g = 0.7, delta = 5.0
        // Expected: 6.7 * 0.7 * 5.0 = 23.45
        Assert.True(rawCme > 0);
        Assert.Equal(23.45, rawCme, 1);
    }

    [Fact]
    public void CalculateRawCme_WithMissingEndTime_UsesDefaultDelta()
    {
        // Arrange
        var calculator = new CmeCalculator();
        var window = new EegWindowRecord
        {
            DeltaPower = 1.0,
            ThetaPower = 1.0,
            AlphaPower = 1.0,
            BetaPower = 1.0,
            ComplexityIndex = 0.5,
            FlowProbability = 0.5
            // No StartUtc/EndUtc - should use default 5.0 seconds
        };

        // Act
        double rawCme = calculator.CalculateRawCme(window);

        // Assert
        Assert.True(rawCme > 0);
        Assert.Equal(5.0, window.GetDeltaSeconds());
    }

    [Fact]
    public void CalculateNormalizationFactor_WithMultipleWindows_ScalesToTarget()
    {
        // Arrange
        var calculator = new CmeCalculator();
        var windows = new List<EegWindowRecord>
        {
            new() { DeltaPower = 1.0, ThetaPower = 1.0, AlphaPower = 1.0, BetaPower = 1.0, 
                    ComplexityIndex = 0.5, FlowProbability = 0.5 },
            new() { DeltaPower = 2.0, ThetaPower = 2.0, AlphaPower = 2.0, BetaPower = 2.0,
                    ComplexityIndex = 0.8, FlowProbability = 0.8 }
        };

        // Act
        double k = calculator.CalculateNormalizationFactor(windows, 100.0);

        // Assert
        Assert.True(k > 0);
        
        // Verify max CME ≈ 100 after normalization
        double maxRaw = windows.Max(w => calculator.CalculateRawCme(w));
        double maxCme = maxRaw * k;
        Assert.True(Math.Abs(maxCme - 100.0) < 1.0); // Within 1% tolerance
    }

    [Fact]
    public void IsFlowWindow_WithHighProbability_ReturnsTrue()
    {
        // Arrange
        var calculator = new CmeCalculator();
        var window = new EegWindowRecord { FlowProbability = 0.75 };

        // Act
        bool isFlow = calculator.IsFlowWindow(window);

        // Assert
        Assert.True(isFlow);
    }

    [Fact]
    public void IsFlowWindow_WithLowProbability_ReturnsFalse()
    {
        // Arrange
        var calculator = new CmeCalculator();
        var window = new EegWindowRecord { FlowProbability = 0.5 };

        // Act
        bool isFlow = calculator.IsFlowWindow(window);

        // Assert
        Assert.False(isFlow); // Threshold is 0.7
    }
}



