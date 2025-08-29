namespace MedjCap.Data.TimeSeries.Models;

/// <summary>
/// Structured time series data with detected temporal patterns.
/// Used by analysis engines to understand data regularity and intervals.
/// </summary>
public record TimeSeriesData
{
    public IEnumerable<DataPoint> DataPoints { get; init; } = Enumerable.Empty<DataPoint>();
    public TimeSpan TimeStep { get; init; } = TimeSpan.Zero;
    public bool IsRegular { get; init; } = false;
}