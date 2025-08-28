namespace MedjCap.Data.Configuration;

/// <summary>
/// Configuration for ML boundary optimization algorithms
/// </summary>
public class OptimizationConfig
{
    /// <summary>
    /// Maximum iterations for optimization algorithms
    /// </summary>
    public int MaxIterations { get; set; } = 1000;

    /// <summary>
    /// Convergence threshold for optimization
    /// </summary>
    public double ConvergenceThreshold { get; set; } = 0.001;

    /// <summary>
    /// Default number of clusters for clustering algorithms
    /// </summary>
    public int DefaultClusterCount { get; set; } = 3;

    /// <summary>
    /// Maximum number of ranges to prevent excessive computation
    /// </summary>
    public int MaxRanges { get; set; } = 100;

    /// <summary>
    /// Maximum tree depth to prevent excessive computation
    /// </summary>
    public int MaxDepth { get; set; } = 20;

    /// <summary>
    /// Performance degradation threshold for stability (30%)
    /// </summary>
    public double PerformanceDegradationThreshold { get; set; } = 0.3;

    /// <summary>
    /// Quantile ranges for boundary optimization
    /// </summary>
    public QuantileRanges Quantiles { get; set; } = new();

    /// <summary>
    /// Trade return scaling factor
    /// </summary>
    public double TradeReturnScale { get; set; } = 0.01;

    /// <summary>
    /// Minimum expected return divisor for risk management
    /// </summary>
    public double MinimumExpectedReturnDivisor { get; set; } = 0.1;

    /// <summary>
    /// Feature importance boost settings
    /// </summary>
    public FeatureImportanceConfig FeatureImportance { get; set; } = new();
}

public class QuantileRanges
{
    /// <summary>
    /// Standard quantile range (25th to 75th percentile)
    /// </summary>
    public (double Lower, double Upper) Standard { get; set; } = (0.25, 0.75);

    /// <summary>
    /// Wider quantile range (20th to 80th percentile)
    /// </summary>
    public (double Lower, double Upper) Wide { get; set; } = (0.2, 0.8);

    /// <summary>
    /// Tertile ranges for three-way splits
    /// </summary>
    public (double First, double Second) Tertiles { get; set; } = (0.33, 0.66);
}

public class FeatureImportanceConfig
{
    /// <summary>
    /// Minimum importance threshold before boosting (35%)
    /// </summary>
    public double MinimumImportanceThreshold { get; set; } = 0.35;

    /// <summary>
    /// Target importance for primary feature (40%)
    /// </summary>
    public double PrimaryFeatureImportance { get; set; } = 0.4;

    /// <summary>
    /// Remaining importance to distribute among other features (60%)
    /// </summary>
    public double RemainingImportance { get; set; } = 0.6;

    /// <summary>
    /// Minimum weight for correlation calculations
    /// </summary>
    public double MinimumWeight { get; set; } = 0.1;
}