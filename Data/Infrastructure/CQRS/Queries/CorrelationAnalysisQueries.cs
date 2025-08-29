using System;
using System.Collections.Generic;

using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.Infrastructure.CQRS.Queries;

/// <summary>
/// Query to calculate price movements for a single time horizon.
/// </summary>
public sealed record CalculatePriceMovementsQuery : BaseQuery<List<PriceMovement>>
{
    /// <summary>
    /// The time series data containing price and indicator information.
    /// </summary>
    public TimeSeriesData TimeSeries { get; init; } = new();

    /// <summary>
    /// The time horizon to analyze.
    /// </summary>
    public TimeSpan TimeHorizon { get; init; }
}

/// <summary>
/// Query to calculate price movements for multiple time horizons.
/// </summary>
public sealed record CalculateMultipleTimeHorizonPriceMovementsQuery : BaseQuery<Dictionary<TimeSpan, List<PriceMovement>>>
{
    /// <summary>
    /// The time series data containing price and indicator information.
    /// </summary>
    public TimeSeriesData TimeSeries { get; init; } = new();

    /// <summary>
    /// Array of time horizons to analyze.
    /// </summary>
    public TimeSpan[] TimeHorizons { get; init; } = Array.Empty<TimeSpan>();
}

/// <summary>
/// Query to calculate correlation between measurements and price movements.
/// </summary>
public sealed record CalculateCorrelationQuery : BaseQuery<CorrelationResult>
{
    /// <summary>
    /// List of price movements with measurement values.
    /// </summary>
    public List<PriceMovement> Movements { get; init; } = new();

    /// <summary>
    /// Type of correlation to calculate.
    /// </summary>
    public CorrelationType CorrelationType { get; init; } = CorrelationType.Pearson;
}

/// <summary>
/// Query to bucketize price movements by ATR targets.
/// </summary>
public sealed record BucketizeMovementsQuery : BaseQuery<Dictionary<string, List<PriceMovement>>>
{
    /// <summary>
    /// List of price movements to bucketize.
    /// </summary>
    public List<PriceMovement> Movements { get; init; } = new();

    /// <summary>
    /// ATR thresholds for creating buckets.
    /// </summary>
    public decimal[] ATRTargets { get; init; } = Array.Empty<decimal>();
}

/// <summary>
/// Query to analyze correlations within specific measurement ranges.
/// </summary>
public sealed record AnalyzeByMeasurementRangesQuery : BaseQuery<Dictionary<string, RangeAnalysisResult>>
{
    /// <summary>
    /// List of price movements to analyze.
    /// </summary>
    public List<PriceMovement> Movements { get; init; } = new();

    /// <summary>
    /// Measurement value ranges to analyze separately.
    /// </summary>
    public List<(decimal Low, decimal High)> MeasurementRanges { get; init; } = new();
}

/// <summary>
/// Query to calculate correlation with contextual filtering.
/// </summary>
public sealed record CalculateCorrelationWithContextualFilterQuery : BaseQuery<CorrelationResult>
{
    /// <summary>
    /// List of price movements to analyze.
    /// </summary>
    public List<PriceMovement> Movements { get; init; } = new();

    /// <summary>
    /// Name of the contextual variable to filter on.
    /// </summary>
    public string ContextVariable { get; init; } = string.Empty;

    /// <summary>
    /// Threshold value for the contextual filter.
    /// </summary>
    public decimal ContextThreshold { get; init; }

    /// <summary>
    /// Comparison operator for filtering.
    /// </summary>
    public ComparisonOperator ComparisonOperator { get; init; } = ComparisonOperator.GreaterThan;
}

/// <summary>
/// Query to run comprehensive correlation analysis.
/// </summary>
public sealed record RunFullCorrelationAnalysisQuery : BaseQuery<CorrelationAnalysisResult>
{
    /// <summary>
    /// The time series data to analyze.
    /// </summary>
    public TimeSeriesData TimeSeries { get; init; } = new();

    /// <summary>
    /// Configuration specifying analysis parameters.
    /// </summary>
    public CorrelationAnalysisRequest Request { get; init; } = new();
}