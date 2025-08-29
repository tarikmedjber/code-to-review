namespace MedjCap.Data.MachineLearning.Optimization.Models;

/// <summary>
/// Defines optimization objectives for ML boundary detection.
/// Determines how the system prioritizes different trading outcomes.
/// </summary>
public enum OptimizationTarget
{
    /// <summary>
    /// Maximize the percentage of correct directional predictions.
    /// </summary>
    HighestWinRate,

    /// <summary>
    /// Optimize for best return per unit of risk (Sharpe-like ratio).
    /// </summary>
    RiskAdjustedReturn,

    /// <summary>
    /// Prioritize consistent performance across different market conditions.
    /// </summary>
    ConsistentResults,

    /// <summary>
    /// Focus on identifying high-probability large price movements (>2 ATR).
    /// </summary>
    LargeMoveProbability,

    /// <summary>
    /// Maximize absolute profit generation.
    /// </summary>
    MaximizeProfit,

    /// <summary>
    /// Minimize risk and drawdown.
    /// </summary>
    MinimizeRisk
}