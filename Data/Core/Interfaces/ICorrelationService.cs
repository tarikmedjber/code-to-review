using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Interface for correlation analysis between indicator measurements and price movements.
/// Provides statistical analysis capabilities for time-series financial data.
/// </summary>
public interface ICorrelationService
{
    // Price Movement Calculation
    /// <summary>
    /// Calculates price movements for a single time horizon from time series data.
    /// </summary>
    /// <param name="timeSeries">The time series data containing price and indicator information</param>
    /// <param name="timeHorizon">The time horizon to analyze (e.g., 15 minutes, 1 hour)</param>
    /// <returns>List of price movements with measurement values and ATR movements</returns>
    List<PriceMovement> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan timeHorizon);
    
    /// <summary>
    /// Calculates price movements for multiple time horizons from time series data.
    /// </summary>
    /// <param name="timeSeries">The time series data containing price and indicator information</param>
    /// <param name="timeHorizons">Array of time horizons to analyze</param>
    /// <returns>Dictionary mapping each time horizon to its corresponding price movements</returns>
    Dictionary<TimeSpan, List<PriceMovement>> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan[] timeHorizons);

    // Correlation Analysis
    /// <summary>
    /// Calculates correlation between measurement values and price movements using specified correlation type.
    /// Includes statistical significance testing and confidence intervals.
    /// </summary>
    /// <param name="movements">List of price movements with measurement values</param>
    /// <param name="correlationType">Type of correlation to calculate (Pearson, Spearman, KendallTau)</param>
    /// <returns>Correlation result with coefficient, p-value, and significance indicators</returns>
    CorrelationResult CalculateCorrelation(List<PriceMovement> movements, CorrelationType correlationType);
    
    // Data Analysis & Segmentation
    /// <summary>
    /// Groups price movements into buckets based on ATR target thresholds for targeted analysis.
    /// </summary>
    /// <param name="movements">List of price movements to bucketize</param>
    /// <param name="atrTargets">ATR thresholds for creating buckets</param>
    /// <returns>Dictionary mapping bucket names to their corresponding price movements</returns>
    Dictionary<string, List<PriceMovement>> BucketizeMovements(List<PriceMovement> movements, decimal[] atrTargets);
    
    /// <summary>
    /// Analyzes correlation within specific measurement value ranges to identify range-dependent relationships.
    /// </summary>
    /// <param name="movements">List of price movements to analyze</param>
    /// <param name="measurementRanges">Measurement value ranges to analyze separately</param>
    /// <returns>Dictionary mapping range identifiers to their analysis results</returns>
    Dictionary<string, RangeAnalysisResult> AnalyzeByMeasurementRanges(List<PriceMovement> movements, List<(decimal Low, decimal High)> measurementRanges);
    
    // Contextual Filtering
    /// <summary>
    /// Calculates correlation with contextual filtering based on market conditions or other variables.
    /// Filters data points based on contextual criteria before performing correlation analysis.
    /// </summary>
    /// <param name="movements">List of price movements to analyze</param>
    /// <param name="contextVariable">Name of the contextual variable to filter on</param>
    /// <param name="contextThreshold">Threshold value for the contextual filter</param>
    /// <param name="comparisonOperator">Comparison operator for filtering (greater than, less than, etc.)</param>
    /// <returns>Correlation result for the filtered dataset</returns>
    CorrelationResult CalculateWithContextualFilter(List<PriceMovement> movements, string contextVariable, decimal contextThreshold, ComparisonOperator comparisonOperator);
    
    // Comprehensive Analysis
    /// <summary>
    /// Runs a comprehensive correlation analysis including multiple time horizons, range analysis,
    /// statistical significance testing, and outlier detection based on the provided request configuration.
    /// </summary>
    /// <param name="timeSeries">The time series data to analyze</param>
    /// <param name="request">Configuration specifying analysis parameters, time horizons, and options</param>
    /// <returns>Complete analysis result with correlations, ranges, statistical tests, and summary</returns>
    CorrelationAnalysisResult RunFullAnalysis(TimeSeriesData timeSeries, CorrelationAnalysisRequest request);
}