using System;
using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.Statistics.Services;
using MedjCap.Data.Analysis.Services;
using MedjCap.Data.MachineLearning.Services;
using MedjCap.Data.DataQuality.Services;
using MedjCap.Data.Backtesting.Services;
using MedjCap.Data.TimeSeries.Services;
using MedjCap.Data.Infrastructure.Caching;
using MedjCap.Data.Infrastructure.MemoryManagement;

namespace MedjCap.Data.Infrastructure.Interfaces;

/// <summary>
/// Service for managing cache invalidation strategies when underlying data changes.
/// Provides event-driven cache invalidation to maintain data consistency.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Registers a cache key with its data dependencies for future invalidation.
    /// </summary>
    /// <param name="cacheKey">The cache key to track</param>
    /// <param name="metadata">Metadata about the cache entry for invalidation decisions</param>
    void RegisterCacheKey(string cacheKey, CacheKeyMetadata metadata);

    /// <summary>
    /// Invalidates cache entries when a new data point is added.
    /// </summary>
    /// <param name="dataPoint">The new data point that was added</param>
    void OnDataPointAdded(DataPoint dataPoint);

    /// <summary>
    /// Invalidates cache entries when time series data is modified.
    /// </summary>
    /// <param name="measurementId">ID of the measurement that changed</param>
    /// <param name="affectedTimeRange">Time range affected by the change (null for all)</param>
    void OnTimeSeriesDataChanged(string measurementId, TimeSpan? affectedTimeRange = null);

    /// <summary>
    /// Invalidates cache entries when configuration changes affect calculations.
    /// </summary>
    /// <param name="changeType">Type of configuration change</param>
    /// <param name="affectedComponent">Specific component affected (optional)</param>
    void OnConfigurationChanged(ConfigurationChangeType changeType, string? affectedComponent = null);

    /// <summary>
    /// Performs bulk invalidation of cache entries based on patterns.
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys</param>
    /// <param name="scope">Scope of pattern matching</param>
    void InvalidateByPattern(string pattern, InvalidationScope scope = InvalidationScope.Exact);

    /// <summary>
    /// Invalidates cache entries older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age for cache entries</param>
    void InvalidateByAge(TimeSpan maxAge);

    /// <summary>
    /// Gets statistics about cache invalidation operations.
    /// </summary>
    /// <returns>Cache invalidation statistics</returns>
    CacheInvalidationStatistics GetStatistics();

    /// <summary>
    /// Clears all invalidation tracking data.
    /// </summary>
    void ClearTrackingData();
}