namespace MedjCap.Data.DataQuality.Models;

/// <summary>
/// Enumeration of available outlier detection methods.
/// Each method uses different statistical approaches to identify anomalous data points.
/// </summary>
public enum OutlierDetectionMethod
{
    /// <summary>
    /// Interquartile Range (IQR) method - detects outliers beyond Q1-1.5*IQR and Q3+1.5*IQR
    /// </summary>
    IQR,

    /// <summary>
    /// Z-Score method - detects outliers beyond ±2 or ±3 standard deviations
    /// </summary>
    ZScore,

    /// <summary>
    /// Modified Z-Score using Median Absolute Deviation (MAD) - more robust to outliers
    /// </summary>
    ModifiedZScore,

    /// <summary>
    /// Isolation Forest method - uses machine learning for anomaly detection
    /// </summary>
    IsolationForest,

    /// <summary>
    /// Combine multiple methods for consensus-based detection
    /// </summary>
    Ensemble
}

/// <summary>
/// Strategy for handling detected outliers.
/// </summary>
public enum OutlierHandlingStrategy
{
    /// <summary>
    /// Remove outlier data points entirely
    /// </summary>
    Remove,

    /// <summary>
    /// Cap outliers at specified percentile boundaries (winsorization)
    /// </summary>
    Cap,

    /// <summary>
    /// Replace outliers with median values
    /// </summary>
    ReplaceWithMedian,

    /// <summary>
    /// Apply logarithmic transformation to reduce outlier impact
    /// </summary>
    LogTransform,

    /// <summary>
    /// Keep outliers but flag them for analysis
    /// </summary>
    Flag
}