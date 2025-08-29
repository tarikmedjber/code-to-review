using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Backtesting.Models;

namespace MedjCap.Data.Backtesting.Interfaces;

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