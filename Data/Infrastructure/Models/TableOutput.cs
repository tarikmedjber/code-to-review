namespace MedjCap.Data.Infrastructure.Models;

/// <summary>
/// Formatted table output for analysis results display.
/// Provides structured data suitable for console, web, or report generation.
/// </summary>
public record TableOutput
{
    public List<string> Headers { get; init; } = new();
    public List<Dictionary<string, object>> Rows { get; init; } = new();
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, string> ColumnFormats { get; init; } = new();
    public List<string> SortColumns { get; init; } = new();
}

/// <summary>
/// Statistical report containing comprehensive analysis metrics.
/// </summary>
public record StatisticalReport
{
    public List<TimeHorizonCorrelation> Correlations { get; init; } = new();
    public int TotalSamples { get; init; }
    public DateRange DateRangeAnalyzed { get; init; } = new();
    public (decimal Low, decimal High) OptimalMeasurementRange { get; init; }
    public TimeSpan BestTimeHorizon { get; init; }
    public double MaxCorrelation { get; init; }
    public Dictionary<string, double> OverallStatistics { get; init; } = new();
}

/// <summary>
/// Correlation statistics for a specific time horizon.
/// </summary>
public record TimeHorizonCorrelation
{
    public TimeSpan TimeHorizon { get; init; }
    public double PearsonCoefficient { get; init; }
    public double SpearmanCoefficient { get; init; }
    public double PValue { get; init; }
    public bool IsSignificant { get; init; }
    public int SampleSize { get; init; }
}