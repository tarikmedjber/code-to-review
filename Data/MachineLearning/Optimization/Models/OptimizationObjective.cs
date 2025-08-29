namespace MedjCap.Data.MachineLearning.Optimization.Models;

/// <summary>
/// Defines an optimization objective for ML boundary optimization.
/// Contains the target goal and configuration parameters for the optimization process.
/// </summary>
public record OptimizationObjective
{
    public OptimizationTarget Target { get; init; }
    public decimal MinATRMove { get; init; }
    public (decimal Low, decimal High) InitialRange { get; init; }
    public double Weight { get; init; } = 1.0;
    public Dictionary<string, object> Parameters { get; init; } = new();
}