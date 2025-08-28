namespace MedjCap.Data.Domain;

/// <summary>
/// Overall statistics for a correlation analysis dataset.
/// Provides summary metrics across all measurements and movements.
/// </summary>
public record OverallStatistics
{
    public int TotalSamples { get; init; }
    public decimal MeanATRMovement { get; init; }
    public decimal StdDevATRMovement { get; init; }
    public decimal MinATRMovement { get; init; }
    public decimal MaxATRMovement { get; init; }
    public double PercentageUpMoves { get; init; }
}