namespace MedjCap.Data.Trading.Models;

/// <summary>
/// Results from validating optimal boundaries on out-of-sample data.
/// Compares in-sample vs out-of-sample performance to detect overfitting.
/// </summary>
public record ValidationResult
{
    public double InSamplePerformance { get; init; }
    public double OutOfSamplePerformance { get; init; }
    public double PerformanceDegradation { get; init; }
    public List<BoundaryValidation> BoundaryPerformance { get; init; } = new();
    public bool IsOverfitted { get; init; }
    public Dictionary<string, double> ValidationMetrics { get; init; } = new();
}

/// <summary>
/// Validation metrics for a specific boundary.
/// </summary>
public record BoundaryValidation
{
    public OptimalBoundary Boundary { get; init; } = new();
    public double InSampleHitRate { get; init; }
    public double OutOfSampleHitRate { get; init; }
    public bool IsStable { get; init; }
    public double StabilityScore { get; init; }
}