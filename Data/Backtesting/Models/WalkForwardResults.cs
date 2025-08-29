using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.Backtesting.Models;

/// <summary>
/// Results from walk-forward analysis showing temporal stability of correlations.
/// Helps detect overfitting and validate model robustness across time periods.
/// </summary>
public record WalkForwardResults
{
    public int WindowCount { get; init; }
    public List<WalkForwardWindow> Windows { get; init; } = new();
    public double AverageCorrelation { get; init; }
    public double CorrelationStdDev { get; init; }
    public bool IsStable { get; init; }
    public double StabilityScore { get; init; }
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
}

/// <summary>
/// Individual window result in walk-forward analysis.
/// </summary>
public record WalkForwardWindow
{
    public DateRange InSamplePeriod { get; init; } = new();
    public DateRange OutOfSamplePeriod { get; init; } = new();
    public double InSampleCorrelation { get; init; }
    public double OutOfSampleCorrelation { get; init; }
    public double PerformanceDegradation { get; init; }
    public int InSampleSize { get; init; }
    public int OutOfSampleSize { get; init; }
    public bool IsSignificant { get; init; }
}

/// <summary>
/// Backtest results for boundary validation.
/// </summary>
public record BacktestResult
{
    public double HitRate { get; init; }
    public double AverageReturn { get; init; }
    public double SharpeRatio { get; init; }
    public double MaxDrawdown { get; init; }
    public int TotalTrades { get; init; }
    public Dictionary<string, double> RiskMetrics { get; init; } = new();
}