namespace MedjCap.Data.Domain;

/// <summary>
/// Result of a correlation coefficient calculation.
/// Includes comprehensive statistical significance testing and confidence intervals.
/// </summary>
public record CorrelationResult
{
    public double Coefficient { get; init; }
    public double PValue { get; init; }
    public int SampleSize { get; init; }
    public bool IsStatisticallySignificant { get; init; }
    public decimal AverageMovement { get; init; }
    public CorrelationType CorrelationType { get; init; }
    
    // Enhanced statistical measures
    public (double Lower, double Upper) ConfidenceInterval { get; init; }
    public double TStatistic { get; init; }
    public int DegreesOfFreedom { get; init; }
    public double StandardError { get; init; }
    
    /// <summary>
    /// Gets a human-readable interpretation of the correlation strength
    /// </summary>
    public string StrengthInterpretation => Math.Abs(Coefficient) switch
    {
        >= 0.8 => "Very Strong",
        >= 0.6 => "Strong", 
        >= 0.4 => "Moderate",
        >= 0.2 => "Weak",
        _ => "Very Weak/None"
    };
    
    /// <summary>
    /// Gets significance level description
    /// </summary>
    public string SignificanceLevel => PValue switch
    {
        < 0.001 => "p < 0.001 (Highly Significant)",
        < 0.01 => "p < 0.01 (Very Significant)",
        < 0.05 => "p < 0.05 (Significant)",
        < 0.10 => "p < 0.10 (Marginally Significant)",
        _ => "Not Significant"
    };
}