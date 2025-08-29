using MedjCap.Data.Trading.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Analysis.Models;

namespace MedjCap.Data.Statistics.Correlation.Models;

/// <summary>
/// Comprehensive result of correlation analysis across multiple time horizons and ranges.
/// Contains all statistical analysis outputs for a measurement type.
/// </summary>
public record CorrelationAnalysisResult
{
    public string MeasurementId { get; init; } = string.Empty;
    public Dictionary<TimeSpan, CorrelationResult> CorrelationsByTimeHorizon { get; init; } = new();
    public Dictionary<string, RangeAnalysisResult> RangeAnalysis { get; init; } = new();
    public Dictionary<string, List<PriceMovement>> ATRBucketAnalysis { get; init; } = new();
    public OverallStatistics OverallStatistics { get; init; } = new();
}