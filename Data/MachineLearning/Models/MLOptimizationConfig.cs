namespace MedjCap.Data.MachineLearning.Models;

/// <summary>
/// Configuration for ML boundary optimization combining multiple algorithms.
/// Specifies which methods to use and their respective parameters.
/// </summary>
public record MLOptimizationConfig
{
    public bool UseDecisionTree { get; init; }
    public bool UseClustering { get; init; }
    public bool UseGradientSearch { get; init; }
    public decimal TargetATRMove { get; init; }
    public int MaxRanges { get; init; }
    public double ValidationRatio { get; init; }
    public int MaxIterations { get; init; } = 1000;
    public double ConvergenceThreshold { get; init; } = 0.001;
    public Dictionary<string, object> AlgorithmParameters { get; init; } = new();
}