namespace MedjCap.Data.Domain;

/// <summary>
/// Result of outlier detection analysis containing identified outliers and statistics.
/// </summary>
public record OutlierDetectionResult
{
    /// <summary>
    /// Method used for outlier detection
    /// </summary>
    public OutlierDetectionMethod Method { get; init; }

    /// <summary>
    /// Indices of detected outliers in the original dataset
    /// </summary>
    public List<int> OutlierIndices { get; init; } = new();

    /// <summary>
    /// Number of outliers detected
    /// </summary>
    public int OutlierCount => OutlierIndices.Count;

    /// <summary>
    /// Percentage of data points identified as outliers
    /// </summary>
    public double OutlierPercentage { get; init; }

    /// <summary>
    /// Threshold values used for detection (varies by method)
    /// </summary>
    public Dictionary<string, double> DetectionThresholds { get; init; } = new();

    /// <summary>
    /// Statistical measures before outlier detection
    /// </summary>
    public DataStatistics OriginalStatistics { get; init; } = new();

    /// <summary>
    /// Statistical measures after excluding outliers
    /// </summary>
    public DataStatistics CleanedStatistics { get; init; } = new();

    /// <summary>
    /// Outlier severity scores (higher = more extreme)
    /// </summary>
    public Dictionary<int, double> OutlierScores { get; init; } = new();

    /// <summary>
    /// Execution time for detection algorithm
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Additional diagnostic information
    /// </summary>
    public List<string> Diagnostics { get; init; } = new();
}

/// <summary>
/// Comprehensive outlier analysis using multiple detection methods.
/// </summary>
public record OutlierAnalysisResult
{
    /// <summary>
    /// Results from individual detection methods
    /// </summary>
    public Dictionary<OutlierDetectionMethod, OutlierDetectionResult> MethodResults { get; init; } = new();

    /// <summary>
    /// Consensus outliers detected by multiple methods
    /// </summary>
    public List<int> ConsensusOutliers { get; init; } = new();

    /// <summary>
    /// Confidence level for each outlier (0-1, higher = more methods agree)
    /// </summary>
    public Dictionary<int, double> OutlierConfidence { get; init; } = new();

    /// <summary>
    /// Recommended outlier handling strategy based on analysis
    /// </summary>
    public OutlierHandlingStrategy RecommendedStrategy { get; init; }

    /// <summary>
    /// Impact assessment of outliers on statistical measures
    /// </summary>
    public OutlierImpactAssessment ImpactAssessment { get; init; } = new();

    /// <summary>
    /// Total analysis execution time
    /// </summary>
    public TimeSpan TotalExecutionTime { get; init; }
}

/// <summary>
/// Assessment of outlier impact on statistical analysis.
/// </summary>
public record OutlierImpactAssessment
{
    /// <summary>
    /// Change in mean after outlier removal
    /// </summary>
    public double MeanImpact { get; init; }

    /// <summary>
    /// Change in standard deviation after outlier removal
    /// </summary>
    public double StandardDeviationImpact { get; init; }

    /// <summary>
    /// Change in correlation coefficients
    /// </summary>
    public double CorrelationImpact { get; init; }

    /// <summary>
    /// Change in skewness after outlier removal
    /// </summary>
    public double SkewnessImpact { get; init; }

    /// <summary>
    /// Change in kurtosis after outlier removal
    /// </summary>
    public double KurtosisImpact { get; init; }

    /// <summary>
    /// Overall impact severity (Low, Medium, High)
    /// </summary>
    public ImpactSeverity Severity { get; init; }
}

/// <summary>
/// Data quality assessment including outlier analysis.
/// </summary>
public record DataQualityReport
{
    /// <summary>
    /// Overall data quality score (0-100)
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Data quality issues identified
    /// </summary>
    public List<DataQualityIssue> Issues { get; init; } = new();

    /// <summary>
    /// Recommendations for data preprocessing
    /// </summary>
    public List<string> Recommendations { get; init; } = new();

    /// <summary>
    /// Statistical summary of data quality metrics
    /// </summary>
    public Dictionary<string, double> QualityMetrics { get; init; } = new();
}

/// <summary>
/// Individual data quality issue.
/// </summary>
public record DataQualityIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public int AffectedDataPoints { get; init; }
    public double ImpactScore { get; init; }
}

/// <summary>
/// Severity levels for impact assessment.
/// </summary>
public enum ImpactSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Severity levels for data quality issues.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}