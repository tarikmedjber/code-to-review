using System;

namespace MedjCap.Data.Infrastructure.Configuration.Options;

/// <summary>
/// Configuration for caching expensive calculations to improve performance
/// </summary>
public class CachingConfig
{
    /// <summary>
    /// Whether caching is enabled globally
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Configuration for correlation calculation caching
    /// </summary>
    public CorrelationCacheConfig CorrelationCache { get; set; } = new();

    /// <summary>
    /// Configuration for statistical significance caching
    /// </summary>
    public StatisticalCacheConfig StatisticalCache { get; set; } = new();

    /// <summary>
    /// Configuration for ML optimization result caching
    /// </summary>
    public OptimizationCacheConfig OptimizationCache { get; set; } = new();

    /// <summary>
    /// Configuration for analysis engine result caching
    /// </summary>
    public AnalysisCacheConfig AnalysisCache { get; set; } = new();
}

/// <summary>
/// Caching configuration for correlation calculations
/// </summary>
public class CorrelationCacheConfig
{
    /// <summary>
    /// Time-to-live for correlation results (default: 30 minutes)
    /// </summary>
    public TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Maximum number of correlation results to cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Whether to cache results by data hash for deterministic caching
    /// </summary>
    public bool UseDataHashing { get; set; } = true;
}

/// <summary>
/// Caching configuration for statistical significance calculations
/// </summary>
public class StatisticalCacheConfig
{
    /// <summary>
    /// Time-to-live for statistical results (default: 1 hour)
    /// </summary>
    public TimeSpan TTL { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of statistical results to cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 500;

    /// <summary>
    /// Cache confidence intervals and p-values separately
    /// </summary>
    public bool CacheDetailedResults { get; set; } = true;
}

/// <summary>
/// Caching configuration for ML optimization results
/// </summary>
public class OptimizationCacheConfig
{
    /// <summary>
    /// Time-to-live for optimization results (default: 2 hours)
    /// </summary>
    public TimeSpan TTL { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Maximum number of optimization results to cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 200;

    /// <summary>
    /// Whether to cache intermediate results during optimization
    /// </summary>
    public bool CacheIntermediateResults { get; set; } = true;
}

/// <summary>
/// Caching configuration for analysis engine results
/// </summary>
public class AnalysisCacheConfig
{
    /// <summary>
    /// Time-to-live for complete analysis results (default: 1 hour)
    /// </summary>
    public TimeSpan TTL { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of analysis results to cache
    /// </summary>
    public int MaxCacheSize { get; set; } = 100;

    /// <summary>
    /// Whether to cache aggregated analysis results
    /// </summary>
    public bool CacheAggregatedResults { get; set; } = true;
}