using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Service for backtesting trading strategies and boundary optimization results.
/// Provides walk-forward analysis and out-of-sample validation capabilities.
/// </summary>
public interface IBacktestService
{
    WalkForwardResults RunWalkForwardAnalysis(List<PriceMovement> movements, AnalysisConfig config, OptimizationTarget target);
    BacktestResult BacktestBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> testData, decimal targetATR);
    List<WalkForwardWindow> CreateWalkForwardWindows(DateRange inSamplePeriod, int windowCount);
}