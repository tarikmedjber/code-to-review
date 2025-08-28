namespace MedjCap.Data.Domain;

/// <summary>
/// Represents an optimal measurement range discovered by ML algorithms.
/// Contains the boundary values and performance metrics for the range.
/// </summary>
public record OptimalBoundary
{
    public decimal RangeLow { get; init; }
    public decimal RangeHigh { get; init; }
    public double Confidence { get; init; }
    public decimal ExpectedATRMove { get; init; }
    public int SampleCount { get; init; }
    public double HitRate { get; init; }
    public double ProbabilityUp { get; init; }
    public string Method { get; init; } = string.Empty;
}