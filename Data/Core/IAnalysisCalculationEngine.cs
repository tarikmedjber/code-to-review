using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Pure calculation engine for analysis operations without orchestration concerns.
/// Handles the core business logic of statistical analysis and ML optimization.
/// </summary>
public interface IAnalysisCalculationEngine
{
    /// <summary>
    /// Performs correlation calculations for a single measurement analysis.
    /// </summary>
    /// <param name="request">Validated analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Correlation calculation results</returns>
    Task<CorrelationCalculationResult> CalculateCorrelationsAsync(
        AnalysisRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs ML boundary optimization calculations.
    /// </summary>
    /// <param name="request">Validated analysis request</param>
    /// <param name="correlationResults">Previous correlation results for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Boundary optimization results</returns>
    Task<BoundaryOptimizationResult> OptimizeBoundariesAsync(
        AnalysisRequest request,
        CorrelationCalculationResult correlationResults,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs walk-forward validation calculations.
    /// </summary>
    /// <param name="request">Validated analysis request</param>
    /// <param name="boundaryResults">Previous boundary optimization results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Walk-forward validation results</returns>
    Task<WalkForwardCalculationResult> ValidateWalkForwardAsync(
        AnalysisRequest request,
        BoundaryOptimizationResult boundaryResults,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs multi-measurement analysis calculations combining multiple indicators.
    /// </summary>
    /// <param name="request">Validated multi-measurement analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-measurement calculation results</returns>
    Task<MultiMeasurementCalculationResult> CalculateMultiMeasurementAsync(
        MultiMeasurementAnalysisRequest request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs contextual analysis calculations examining context variable effects.
    /// </summary>
    /// <param name="request">Validated contextual analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contextual analysis calculation results</returns>
    Task<ContextualCalculationResult> CalculateContextualAsync(
        ContextualAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Results from correlation calculations.
/// </summary>
public record CorrelationCalculationResult
{
    /// <summary>
    /// Correlation results by time horizon.
    /// </summary>
    public Dictionary<TimeSpan, CorrelationResult> CorrelationsByHorizon { get; init; } = new();
    
    /// <summary>
    /// Price movements data used for calculations.
    /// </summary>
    public List<PriceMovement> PriceMovements { get; init; } = new();
    
    /// <summary>
    /// Data points filtered for the analysis period.
    /// </summary>
    public List<DataPoint> FilteredDataPoints { get; init; } = new();
    
    /// <summary>
    /// Time series data used for calculations.
    /// </summary>
    public TimeSeriesData TimeSeriesData { get; init; } = new();
}

/// <summary>
/// Results from boundary optimization calculations.
/// </summary>
public record BoundaryOptimizationResult
{
    /// <summary>
    /// Optimal boundaries discovered by ML optimization.
    /// </summary>
    public List<OptimalBoundary> OptimalBoundaries { get; init; } = new();
    
    /// <summary>
    /// ML optimization method that produced the best results.
    /// </summary>
    public string BestMethod { get; init; } = string.Empty;
    
    /// <summary>
    /// Cross-validation results for the optimization.
    /// </summary>
    public CrossValidationResult? CrossValidationResults { get; init; }
    
    /// <summary>
    /// Performance metrics for the optimization.
    /// </summary>
    public Dictionary<string, double> OptimizationMetrics { get; init; } = new();
}

/// <summary>
/// Results from walk-forward validation calculations.
/// </summary>
public record WalkForwardCalculationResult
{
    /// <summary>
    /// Walk-forward validation results.
    /// </summary>
    public WalkForwardResults WalkForwardResults { get; init; } = new();
    
    /// <summary>
    /// Validation windows used for analysis.
    /// </summary>
    public List<WalkForwardWindow> ValidationWindows { get; init; } = new();
    
    /// <summary>
    /// Out-of-sample performance metrics.
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
}

/// <summary>
/// Results from multi-measurement analysis calculations.
/// </summary>
public record MultiMeasurementCalculationResult
{
    /// <summary>
    /// Optimal weights for combining measurements.
    /// </summary>
    public Dictionary<string, double> OptimalWeights { get; init; } = new();
    
    /// <summary>
    /// Individual correlation for each measurement.
    /// </summary>
    public Dictionary<string, double> IndividualCorrelations { get; init; } = new();
    
    /// <summary>
    /// Combined correlation after optimal weighting.
    /// </summary>
    public double CombinedCorrelation { get; init; }
    
    /// <summary>
    /// Measurement importance rankings.
    /// </summary>
    public Dictionary<string, double> MeasurementImportance { get; init; } = new();
    
    /// <summary>
    /// Combined optimal boundaries.
    /// </summary>
    public List<OptimalBoundary> CombinedBoundaries { get; init; } = new();
}

/// <summary>
/// Results from contextual analysis calculations.
/// </summary>
public record ContextualCalculationResult
{
    /// <summary>
    /// Analysis results grouped by context variable ranges.
    /// </summary>
    public List<ContextGroup> ContextGroups { get; init; } = new();
    
    /// <summary>
    /// Context variable being analyzed.
    /// </summary>
    public string ContextVariable { get; init; } = string.Empty;
    
    /// <summary>
    /// Overall effect of context on correlation strength.
    /// </summary>
    public double OverallContextEffect { get; init; }
    
    /// <summary>
    /// Statistical significance of context effects.
    /// </summary>
    public Dictionary<string, double> ContextSignificance { get; init; } = new();
}