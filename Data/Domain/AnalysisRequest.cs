namespace MedjCap.Data.Domain;

/// <summary>
/// Request configuration for single-measurement statistical analysis.
/// Contains all parameters needed to run comprehensive correlation and ML analysis.
/// </summary>
public record AnalysisRequest
{
    public string MeasurementId { get; init; } = string.Empty;
    public TimeSpan[] TimeHorizons { get; init; } = Array.Empty<TimeSpan>();
    public decimal[] ATRTargets { get; init; } = Array.Empty<decimal>();
    public OptimizationTarget OptimizationTarget { get; init; }
    public AnalysisConfig Config { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Request configuration for multi-measurement analysis combining multiple indicators.
/// </summary>
public record MultiMeasurementAnalysisRequest
{
    public string[] MeasurementIds { get; init; } = Array.Empty<string>();
    public TimeSpan[] TimeHorizons { get; init; } = Array.Empty<TimeSpan>();
    public decimal[] ATRTargets { get; init; } = Array.Empty<decimal>();
    public OptimizationTarget OptimizationTarget { get; init; }
    public AnalysisConfig Config { get; init; } = new();
    public bool FindOptimalWeights { get; init; } = true;
    public Dictionary<string, double> InitialWeights { get; init; } = new();
}

/// <summary>
/// Request configuration for contextual analysis examining how context variables affect correlations.
/// </summary>
public record ContextualAnalysisRequest
{
    public string PrimaryMeasurement { get; init; } = string.Empty;
    public string ContextVariable { get; init; } = string.Empty;
    public decimal[] ContextThresholds { get; init; } = Array.Empty<decimal>();
    public TimeSpan TimeHorizon { get; init; }
    public AnalysisConfig Config { get; init; } = new();
}