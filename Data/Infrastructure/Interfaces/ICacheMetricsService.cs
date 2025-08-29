using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedjCap.Data.Infrastructure.Interfaces;

/// <summary>
/// Service for collecting and reporting cache performance metrics and telemetry.
/// </summary>
public interface ICacheMetricsService
{
    /// <summary>
    /// Records a cache operation with timing and result information.
    /// </summary>
    /// <param name="cacheType">Type of cache (correlation, optimization, etc.)</param>
    /// <param name="operation">Operation performed (get, set, etc.)</param>
    /// <param name="wasHit">Whether the operation was a cache hit</param>
    /// <param name="duration">Time taken for the operation</param>
    /// <param name="dataSize">Size of data involved (optional)</param>
    void RecordCacheOperation(string cacheType, string operation, bool wasHit, TimeSpan duration, int? dataSize = null);

    /// <summary>
    /// Records a cache invalidation event.
    /// </summary>
    /// <param name="cacheType">Type of cache being invalidated</param>
    /// <param name="keysInvalidated">Number of keys that were invalidated</param>
    /// <param name="reason">Reason for invalidation (optional)</param>
    void RecordCacheInvalidation(string cacheType, int keysInvalidated, string? reason = null);

    /// <summary>
    /// Gets comprehensive cache performance metrics.
    /// </summary>
    /// <returns>Complete performance metrics</returns>
    CachePerformanceMetrics GetPerformanceMetrics();

    /// <summary>
    /// Gets metrics for a specific cache type.
    /// </summary>
    /// <param name="cacheType">The cache type to get metrics for</param>
    /// <returns>Metrics for the specified cache type, or null if not found</returns>
    CacheTypeMetrics? GetMetricsForCacheType(string cacheType);

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    void ResetMetrics();

    /// <summary>
    /// Starts periodic logging of cache metrics for monitoring.
    /// </summary>
    /// <param name="interval">Interval between metric reports</param>
    /// <param name="cancellationToken">Token to cancel periodic reporting</param>
    /// <returns>Task representing the periodic reporting operation</returns>
    Task StartPeriodicReporting(TimeSpan interval, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comprehensive cache performance metrics.
/// </summary>
public class CachePerformanceMetrics
{
    /// <summary>
    /// Total number of cache operations performed.
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public long TotalHits { get; init; }

    /// <summary>
    /// Total number of cache misses.
    /// </summary>
    public long TotalMisses { get; init; }

    /// <summary>
    /// Total number of cache invalidations.
    /// </summary>
    public long TotalInvalidations { get; init; }

    /// <summary>
    /// Overall hit rate across all cache types.
    /// </summary>
    public double OverallHitRate { get; init; }

    /// <summary>
    /// Overall miss rate across all cache types.
    /// </summary>
    public double OverallMissRate { get; init; }

    /// <summary>
    /// When these metrics were collected.
    /// </summary>
    public DateTime MetricsCollectedAt { get; init; }

    /// <summary>
    /// Detailed metrics for each operation type.
    /// </summary>
    public List<CacheOperationMetric> OperationMetrics { get; init; } = new();

    /// <summary>
    /// Hit rates by cache type.
    /// </summary>
    public Dictionary<string, double> HitRatesByType { get; init; } = new();

    /// <summary>
    /// Average duration across all cache operations.
    /// </summary>
    public TimeSpan AverageOperationDuration { get; init; }

    /// <summary>
    /// Slowest cache operations (for performance optimization).
    /// </summary>
    public List<CacheOperationMetric> SlowestOperations { get; init; } = new();

    /// <summary>
    /// Most frequently used cache operations.
    /// </summary>
    public List<CacheOperationMetric> MostActiveOperations { get; init; } = new();

    /// <summary>
    /// Overall performance score (0-100).
    /// </summary>
    public int PerformanceScore { get; init; }

    /// <summary>
    /// Recommended actions for improving cache performance.
    /// </summary>
    public List<string> RecommendedActions { get; init; } = new();
}

/// <summary>
/// Metrics for a specific cache type.
/// </summary>
public class CacheTypeMetrics
{
    /// <summary>
    /// Cache type identifier.
    /// </summary>
    public string CacheType { get; init; } = string.Empty;

    /// <summary>
    /// Total operations for this cache type.
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Number of hits for this cache type.
    /// </summary>
    public long Hits { get; init; }

    /// <summary>
    /// Number of misses for this cache type.
    /// </summary>
    public long Misses { get; init; }

    /// <summary>
    /// Hit rate for this cache type.
    /// </summary>
    public double HitRate { get; init; }

    /// <summary>
    /// Average operation duration for this cache type.
    /// </summary>
    public TimeSpan AverageOperationDuration { get; init; }

    /// <summary>
    /// Individual operations within this cache type.
    /// </summary>
    public List<CacheOperationMetric> Operations { get; init; } = new();
}

/// <summary>
/// Detailed metrics for a specific cache operation.
/// </summary>
public class CacheOperationMetric
{
    /// <summary>
    /// Cache type (correlation, optimization, etc.).
    /// </summary>
    public string CacheType { get; init; } = string.Empty;

    /// <summary>
    /// Operation name (CalculateCorrelation, OptimizeBoundaries, etc.).
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Total number of times this operation was performed.
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Number of cache hits for this operation.
    /// </summary>
    public long Hits { get; init; }

    /// <summary>
    /// Number of cache misses for this operation.
    /// </summary>
    public long Misses { get; init; }

    /// <summary>
    /// Hit rate for this specific operation.
    /// </summary>
    public double HitRate => TotalOperations > 0 ? (double)Hits / TotalOperations : 0.0;

    /// <summary>
    /// Total time spent on this operation across all calls.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Average duration per operation call.
    /// </summary>
    public TimeSpan AverageDuration => TotalOperations > 0 
        ? TimeSpan.FromTicks(TotalDuration.Ticks / TotalOperations) 
        : TimeSpan.Zero;

    /// <summary>
    /// Fastest operation time recorded.
    /// </summary>
    public TimeSpan MinDuration { get; init; }

    /// <summary>
    /// Slowest operation time recorded.
    /// </summary>
    public TimeSpan MaxDuration { get; init; }

    /// <summary>
    /// When this operation was last performed.
    /// </summary>
    public DateTime LastOperation { get; init; }

    /// <summary>
    /// Average data size involved in operations (if tracked).
    /// </summary>
    public double AverageDataSize { get; init; }
}