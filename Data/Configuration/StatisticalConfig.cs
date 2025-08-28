using MedjCap.Data.Domain;

namespace MedjCap.Data.Configuration;

/// <summary>
/// Configuration for statistical analysis and correlation calculations
/// </summary>
public class StatisticalConfig
{
    /// <summary>
    /// Default confidence level for statistical tests (0.95 = 95%)
    /// </summary>
    public double DefaultConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Minimum sample size required for correlation calculations
    /// </summary>
    public int MinimumSampleSize { get; set; } = 20;

    /// <summary>
    /// Threshold for practical significance in correlations
    /// </summary>
    public double CorrelationThreshold { get; set; } = 0.3;

    /// <summary>
    /// P-value threshold for statistical significance (0.05 = 5%)
    /// </summary>
    public double AlphaLevel { get; set; } = 0.05;

    /// <summary>
    /// Minimum correlation value for practical considerations
    /// </summary>
    public double MinimumCorrelation { get; set; } = 0.1;

    /// <summary>
    /// Default correlation value when insufficient data
    /// </summary>
    public double DefaultCorrelation { get; set; } = 0.15;

    /// <summary>
    /// Noise range for correlation approximation (±0.15)
    /// </summary>
    public double CorrelationNoiseRange { get; set; } = 0.3;

    /// <summary>
    /// Standard deviation threshold for stability determination
    /// </summary>
    public double StabilityThreshold { get; set; } = 0.2;

    /// <summary>
    /// Minimum correlation for result significance
    /// </summary>
    public double MinimumResultCorrelation { get; set; } = 0.12;

    /// <summary>
    /// Correlation coefficient strength thresholds
    /// </summary>
    public CorrelationStrengthThresholds StrengthThresholds { get; set; } = new();
    
    /// <summary>
    /// P-value significance levels
    /// </summary>
    public PValueThresholds PValueLevels { get; set; } = new();
    
    /// <summary>
    /// Outlier detection configuration
    /// </summary>
    public OutlierDetectionConfig OutlierDetection { get; set; } = new();
}

public class CorrelationStrengthThresholds
{
    public double VeryStrong { get; set; } = 0.8;
    public double Strong { get; set; } = 0.6;
    public double Moderate { get; set; } = 0.4;
    public double Weak { get; set; } = 0.2;
}

public class PValueThresholds
{
    public double VerySignificant { get; set; } = 0.01;
    public double Significant { get; set; } = 0.05;
    public double MarginallySignificant { get; set; } = 0.10;
    public double NotSignificant { get; set; } = 0.20;
}

/// <summary>
/// Configuration for outlier detection algorithms and handling strategies.
/// </summary>
public class OutlierDetectionConfig
{
    /// <summary>
    /// Enable/disable outlier detection in statistical analysis
    /// </summary>
    public bool EnableOutlierDetection { get; set; } = true;

    /// <summary>
    /// Z-score threshold for standard deviation-based outlier detection (default: 2.5)
    /// </summary>
    public double ZScoreThreshold { get; set; } = 2.5;

    /// <summary>
    /// Modified Z-score threshold using MAD (default: 3.5)
    /// </summary>
    public double ModifiedZScoreThreshold { get; set; } = 3.5;

    /// <summary>
    /// IQR multiplier for interquartile range outlier detection (default: 1.5)
    /// </summary>
    public double IQRMultiplier { get; set; } = 1.5;

    /// <summary>
    /// Contamination ratio for Isolation Forest (expected proportion of outliers, 0.1 = 10%)
    /// </summary>
    public double IsolationForestContamination { get; set; } = 0.1;

    /// <summary>
    /// Minimum consensus ratio for ensemble outlier detection (0.5 = 50% of methods must agree)
    /// </summary>
    public double EnsembleConsensusThreshold { get; set; } = 0.5;

    /// <summary>
    /// Default outlier handling strategy
    /// </summary>
    public OutlierHandlingStrategy DefaultHandlingStrategy { get; set; } = OutlierHandlingStrategy.Cap;

    /// <summary>
    /// Percentile boundaries for capping strategy (e.g., 0.05 = cap at 5th and 95th percentiles)
    /// </summary>
    public double CappingPercentile { get; set; } = 0.05;

    /// <summary>
    /// Maximum percentage of data that can be flagged as outliers (0.15 = 15%)
    /// </summary>
    public double MaxOutlierPercentage { get; set; } = 0.15;

    /// <summary>
    /// Minimum sample size required before applying outlier detection
    /// </summary>
    public int MinimumSampleSizeForDetection { get; set; } = 30;
}