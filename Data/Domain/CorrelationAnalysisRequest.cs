namespace MedjCap.Data.Domain;

/// <summary>
/// Request configuration for comprehensive correlation analysis.
/// Defines what measurements to analyze and how to segment the analysis.
/// </summary>
public record CorrelationAnalysisRequest
{
    public string MeasurementId { get; init; } = string.Empty;
    public TimeSpan[] TimeHorizons { get; init; } = Array.Empty<TimeSpan>();
    public decimal[] ATRTargets { get; init; } = Array.Empty<decimal>();
    public List<(decimal Low, decimal High)> MeasurementRanges { get; init; } = new();
}