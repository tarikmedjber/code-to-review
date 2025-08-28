namespace MedjCap.Data.Domain;

/// <summary>
/// Represents an optimal range discovered through gradient-based optimization.
/// Contains the optimized boundaries and achieved objective value.
/// </summary>
public record OptimalRange
{
    public decimal Low { get; init; }
    public decimal High { get; init; }
    public double ObjectiveValue { get; init; }
    public int IterationsUsed { get; init; }
    public bool Converged { get; init; }
    public Dictionary<string, double> AdditionalMetrics { get; init; } = new();
}