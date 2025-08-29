using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Trading.Models;

namespace MedjCap.Data.Statistics.Models;

/// <summary>
/// Statistical summary of collected data points.
/// Provides counts, ranges, and metadata for analysis preparation.
/// </summary>
public record DataStatistics
{
    public int TotalDataPoints { get; init; }
    public int UniqueTimestamps { get; init; }
    public IEnumerable<string> UniqueMeasurementIds { get; init; } = Enumerable.Empty<string>();
    public DateRange DateRange { get; init; } = new();
    public PriceRange PriceRange { get; init; } = new();
    
    // Additional statistical measures for outlier detection
    public int SampleCount { get; init; }
    public decimal MeasurementMean { get; init; }
    public decimal MeasurementStdDev { get; init; }
    public decimal MeasurementMin { get; init; }
    public decimal MeasurementMax { get; init; }
    public decimal ATRMean { get; init; }
    public decimal ATRStdDev { get; init; }
    public decimal ATRMin { get; init; }
    public decimal ATRMax { get; init; }
}