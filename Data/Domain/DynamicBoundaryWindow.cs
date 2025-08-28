namespace MedjCap.Data.Domain;

/// <summary>
/// Represents optimal boundaries discovered within a specific time window.
/// Used for detecting regime changes and time-varying optimal ranges.
/// </summary>
public record DynamicBoundaryWindow
{
    public DateTime WindowStart { get; init; }
    public DateTime WindowEnd { get; init; }
    public OptimalRange OptimalRange { get; init; } = new();
    public double Confidence { get; init; }
    public int SampleSize { get; init; }
    public bool RegimeChange { get; init; }
    public double StabilityScore { get; init; }
}