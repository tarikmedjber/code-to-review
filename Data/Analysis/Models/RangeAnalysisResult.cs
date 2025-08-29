namespace MedjCap.Data.Analysis.Models;

/// <summary>
/// Analysis result for a specific measurement range.
/// Shows probability and average movement statistics for that range.
/// </summary>
public record RangeAnalysisResult
{
    public double ProbabilityUp { get; init; }
    public double ProbabilityDown { get; init; }
    public decimal AverageATRMove { get; init; }
    public int SampleCount { get; init; }
    public decimal MinMovement { get; init; }
    public decimal MaxMovement { get; init; }
}