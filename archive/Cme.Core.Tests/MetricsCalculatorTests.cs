using Cme.Core;
using Xunit;

namespace Cme.Core.Tests;

public class MetricsCalculatorTests
{
    [Fact]
    public void CalculateMetrics_WithSingleSession_ComputesCorrectly()
    {
        // Arrange
        var calculator = new MetricsCalculator();
        var windows = new List<EegWindowRecord>
        {
            new()
            {
                SessionId = "session1",
                DeltaPower = 1.0,
                ThetaPower = 1.0,
                AlphaPower = 1.0,
                BetaPower = 1.0,
                ComplexityIndex = 0.5,
                FlowProbability = 0.8, // Flow window
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddSeconds(5.0)
            },
            new()
            {
                SessionId = "session1",
                DeltaPower = 1.0,
                ThetaPower = 1.0,
                AlphaPower = 1.0,
                BetaPower = 1.0,
                ComplexityIndex = 0.5,
                FlowProbability = 0.5, // Not flow
                StartUtc = DateTime.UtcNow.AddSeconds(5),
                EndUtc = DateTime.UtcNow.AddSeconds(10)
            }
        };

        // Act
        var (sessions, global, k) = calculator.CalculateMetrics(windows);

        // Assert
        Assert.Single(sessions);
        var session = sessions[0];
        Assert.Equal("session1", session.SessionId);
        Assert.Equal(2, session.TotalWindows);
        Assert.Equal(1, session.FlowWindows);
        Assert.Equal(0.5, session.FlowShare, 1);
        Assert.Equal(10.0, session.TotalDurationSeconds, 1);
        Assert.Equal(5.0, session.FlowDurationSeconds, 1);
        Assert.True(session.CmeSession > 0);
        Assert.Equal(1, global.TotalSessions);
    }

    [Fact]
    public void CalculateMetrics_WithMultipleSessions_GroupsCorrectly()
    {
        // Arrange
        var calculator = new MetricsCalculator();
        var windows = new List<EegWindowRecord>
        {
            new() { SessionId = "session1", DeltaPower = 1.0, ThetaPower = 1.0, AlphaPower = 1.0, BetaPower = 1.0,
                    ComplexityIndex = 0.5, FlowProbability = 0.5 },
            new() { SessionId = "session1", DeltaPower = 1.0, ThetaPower = 1.0, AlphaPower = 1.0, BetaPower = 1.0,
                    ComplexityIndex = 0.5, FlowProbability = 0.5 },
            new() { SessionId = "session2", DeltaPower = 1.0, ThetaPower = 1.0, AlphaPower = 1.0, BetaPower = 1.0,
                    ComplexityIndex = 0.5, FlowProbability = 0.5 }
        };

        // Act
        var (sessions, global, _) = calculator.CalculateMetrics(windows);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Equal(2, global.TotalSessions);
    }

    [Fact]
    public void CalculateMetrics_WithEmptyList_ReturnsEmptyResults()
    {
        // Arrange
        var calculator = new MetricsCalculator();
        var windows = new List<EegWindowRecord>();

        // Act
        var (sessions, global, k) = calculator.CalculateMetrics(windows);

        // Assert
        Assert.Empty(sessions);
        Assert.Equal(0, global.TotalSessions);
        Assert.Equal(1.0, k);
    }
}



