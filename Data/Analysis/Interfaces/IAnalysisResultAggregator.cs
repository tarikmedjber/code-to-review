using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;

namespace MedjCap.Data.Analysis.Interfaces;

/// <summary>
/// Aggregates calculation results into final analysis results.
/// Handles the combining of correlation, optimization, and validation results.
/// </summary>
public interface IAnalysisResultAggregator
{
    /// <summary>
    /// Aggregates single measurement analysis results.
    /// </summary>
    /// <param name="request">Original analysis request</param>
    /// <param name="correlationResults">Correlation calculation results</param>
    /// <param name="boundaryResults">Boundary optimization results</param>
    /// <param name="walkForwardResults">Walk-forward validation results</param>
    /// <returns>Complete analysis result</returns>
    AnalysisResult AggregateResults(
        AnalysisRequest request,
        CorrelationCalculationResult correlationResults,
        BoundaryOptimizationResult boundaryResults,
        WalkForwardCalculationResult walkForwardResults);
    
    /// <summary>
    /// Aggregates multi-measurement analysis results.
    /// </summary>
    /// <param name="request">Original multi-measurement analysis request</param>
    /// <param name="calculationResults">Multi-measurement calculation results</param>
    /// <returns>Complete multi-measurement analysis result</returns>
    MultiMeasurementAnalysisResult AggregateResults(
        MultiMeasurementAnalysisRequest request,
        MultiMeasurementCalculationResult calculationResults);
    
    /// <summary>
    /// Aggregates contextual analysis results.
    /// </summary>
    /// <param name="request">Original contextual analysis request</param>
    /// <param name="calculationResults">Contextual calculation results</param>
    /// <returns>Complete contextual analysis result</returns>
    ContextualAnalysisResult AggregateResults(
        ContextualAnalysisRequest request,
        ContextualCalculationResult calculationResults);
    
    /// <summary>
    /// Calculates overall analysis quality metrics from constituent results.
    /// </summary>
    /// <param name="results">Analysis results to evaluate</param>
    /// <returns>Quality metrics and confidence scores</returns>
    AnalysisQualityMetrics CalculateQualityMetrics(AnalysisResult results);
}

/// <summary>
/// Quality metrics for analysis results.
/// </summary>
public record AnalysisQualityMetrics
{
    /// <summary>
    /// Overall confidence in the analysis results (0.0-1.0).
    /// </summary>
    public double OverallConfidence { get; init; }
    
    /// <summary>
    /// Data quality score based on sample sizes and completeness.
    /// </summary>
    public double DataQualityScore { get; init; }
    
    /// <summary>
    /// Statistical significance of the findings.
    /// </summary>
    public double StatisticalSignificance { get; init; }
    
    /// <summary>
    /// Stability score based on walk-forward validation.
    /// </summary>
    public double StabilityScore { get; init; }
    
    /// <summary>
    /// Risk assessment for using these results in trading.
    /// </summary>
    public RiskAssessment RiskAssessment { get; init; } = new();
    
    /// <summary>
    /// Individual quality metrics by analysis component.
    /// </summary>
    public Dictionary<string, double> ComponentQuality { get; init; } = new();
    
    /// <summary>
    /// Recommendations for improving analysis quality.
    /// </summary>
    public List<string> QualityRecommendations { get; init; } = new();
}

/// <summary>
/// Risk assessment for analysis results.
/// </summary>
public record RiskAssessment
{
    /// <summary>
    /// Overall risk level (Low, Medium, High).
    /// </summary>
    public RiskLevel RiskLevel { get; init; }
    
    /// <summary>
    /// Specific risk factors identified.
    /// </summary>
    public List<RiskFactor> RiskFactors { get; init; } = new();
    
    /// <summary>
    /// Risk mitigation suggestions.
    /// </summary>
    public List<string> MitigationSuggestions { get; init; } = new();
    
    /// <summary>
    /// Quantitative risk metrics.
    /// </summary>
    public Dictionary<string, double> RiskMetrics { get; init; } = new();
}

/// <summary>
/// Risk levels for analysis results.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk - high confidence, stable results.
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium risk - moderate confidence, some instability.
    /// </summary>
    Medium,
    
    /// <summary>
    /// High risk - low confidence, unstable results.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical risk - unreliable results, not suitable for trading.
    /// </summary>
    Critical
}

/// <summary>
/// Specific risk factors in analysis results.
/// </summary>
public record RiskFactor
{
    /// <summary>
    /// Type of risk factor.
    /// </summary>
    public RiskFactorType Type { get; init; }
    
    /// <summary>
    /// Description of the risk factor.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Severity of the risk factor (0.0-1.0).
    /// </summary>
    public double Severity { get; init; }
    
    /// <summary>
    /// Impact on overall analysis reliability.
    /// </summary>
    public double Impact { get; init; }
}

/// <summary>
/// Types of risk factors in analysis.
/// </summary>
public enum RiskFactorType
{
    /// <summary>
    /// Insufficient data for reliable analysis.
    /// </summary>
    InsufficientData,
    
    /// <summary>
    /// High performance degradation in validation.
    /// </summary>
    PerformanceDegradation,
    
    /// <summary>
    /// Statistical insignificance of results.
    /// </summary>
    StatisticalInsignificance,
    
    /// <summary>
    /// High correlation instability across time periods.
    /// </summary>
    CorrelationInstability,
    
    /// <summary>
    /// Overfitting detected in ML optimization.
    /// </summary>
    Overfitting,
    
    /// <summary>
    /// Data quality issues affecting reliability.
    /// </summary>
    DataQuality,
    
    /// <summary>
    /// Market regime changes affecting results.
    /// </summary>
    RegimeChange
}