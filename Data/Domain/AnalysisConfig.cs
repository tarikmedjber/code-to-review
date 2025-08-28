namespace MedjCap.Data.Domain;

/// <summary>
/// Configuration for statistical analysis and validation parameters.
/// Defines time periods, sample sizes, and confidence levels for correlation testing.
/// </summary>
public record AnalysisConfig
{
    public DateRange InSample { get; init; } = new();
    public List<DateRange> OutOfSamples { get; init; } = new();
    public int WalkForwardWindows { get; init; } = 10;
    public int MinSampleSize { get; init; } = 50;
    public double ConfidenceLevel { get; init; } = 0.95;

    /// <summary>
    /// Validates the configuration for logical consistency.
    /// </summary>
    public bool IsValid()
    {
        if (InSample.Start >= InSample.End) return false;
        if (WalkForwardWindows <= 0) return false;
        if (MinSampleSize <= 0) return false;
        if (ConfidenceLevel <= 0 || ConfidenceLevel >= 1) return false;
        
        // Check that out-of-sample periods don't overlap with in-sample
        return OutOfSamples.All(oos => !oos.Overlaps(InSample));
    }
}

