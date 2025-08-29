using MedjCap.Data.Trading.Models;

namespace MedjCap.Data.MachineLearning.Models;

/// <summary>
/// Represents the result of clustering analysis on price movements.
/// Contains cluster characteristics and member measurements.
/// </summary>
public record ClusterResult
{
    public decimal CenterMeasurement { get; init; }
    public decimal AverageATRMove { get; init; }
    public int MemberCount { get; init; }
    public List<PriceMovement> Members { get; init; } = new();
    public double WithinClusterVariance { get; init; }
    public (decimal Low, decimal High) BoundaryRange { get; init; }
}