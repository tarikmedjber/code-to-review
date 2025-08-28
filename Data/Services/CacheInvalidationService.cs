using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Services;

/// <summary>
/// Manages cache invalidation strategies for data analysis services.
/// Provides event-driven cache invalidation when underlying data changes.
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly CachingConfig _config;
    private readonly ILogger<CacheInvalidationService> _logger;
    
    // Track cache keys by data hash for efficient invalidation
    private readonly ConcurrentDictionary<string, HashSet<string>> _dataHashToCacheKeys = new();
    private readonly ConcurrentDictionary<string, DateTime> _keyCreationTimes = new();
    private readonly ConcurrentDictionary<string, CacheKeyMetadata> _keyMetadata = new();
    
    public CacheInvalidationService(
        IMemoryCache cache,
        IOptions<CachingConfig> config,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a cache key with its data dependencies for future invalidation.
    /// </summary>
    public void RegisterCacheKey(string cacheKey, CacheKeyMetadata metadata)
    {
        if (string.IsNullOrEmpty(cacheKey) || metadata == null)
            return;

        _keyMetadata.TryAdd(cacheKey, metadata);
        _keyCreationTimes.TryAdd(cacheKey, DateTime.UtcNow);

        // Index by data hash for efficient lookup
        if (!string.IsNullOrEmpty(metadata.DataHash))
        {
            _dataHashToCacheKeys.AddOrUpdate(
                metadata.DataHash,
                new HashSet<string> { cacheKey },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(cacheKey);
                        return existing;
                    }
                });
        }

        // Index by measurement ID for measurement-specific invalidation
        if (!string.IsNullOrEmpty(metadata.MeasurementId))
        {
            var measurementKey = $"measurement:{metadata.MeasurementId}";
            _dataHashToCacheKeys.AddOrUpdate(
                measurementKey,
                new HashSet<string> { cacheKey },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(cacheKey);
                        return existing;
                    }
                });
        }

        _logger.LogDebug("Registered cache key {CacheKey} with dependencies: {DataHash}, {MeasurementId}",
            cacheKey, metadata.DataHash, metadata.MeasurementId);
    }

    /// <summary>
    /// Invalidates cache entries when data points are added or modified.
    /// </summary>
    public void OnDataPointAdded(DataPoint dataPoint)
    {
        if (dataPoint == null)
            return;

        var invalidatedKeys = new HashSet<string>();

        // Invalidate by measurement ID
        var measurementKey = $"measurement:{dataPoint.MeasurementId}";
        if (_dataHashToCacheKeys.TryGetValue(measurementKey, out var measurementKeys))
        {
            lock (measurementKeys)
            {
                foreach (var key in measurementKeys.ToList())
                {
                    InvalidateCacheKey(key);
                    invalidatedKeys.Add(key);
                }
            }
        }

        // Invalidate time-based entries (data from similar time periods)
        var timeBasedKey = $"timeframe:{dataPoint.Timestamp:yyyyMMddHH}";
        if (_dataHashToCacheKeys.TryGetValue(timeBasedKey, out var timeKeys))
        {
            lock (timeKeys)
            {
                foreach (var key in timeKeys.ToList())
                {
                    InvalidateCacheKey(key);
                    invalidatedKeys.Add(key);
                }
            }
        }

        if (invalidatedKeys.Count > 0)
        {
            _logger.LogInformation("Data point added for {MeasurementId}, invalidated {Count} cache entries",
                dataPoint.MeasurementId, invalidatedKeys.Count);
        }
    }

    /// <summary>
    /// Invalidates cache entries when time series data is modified.
    /// </summary>
    public void OnTimeSeriesDataChanged(string measurementId, TimeSpan? affectedTimeRange = null)
    {
        if (string.IsNullOrEmpty(measurementId))
            return;

        var invalidatedKeys = new HashSet<string>();

        // Find all cache keys related to this measurement
        var measurementKey = $"measurement:{measurementId}";
        if (_dataHashToCacheKeys.TryGetValue(measurementKey, out var keys))
        {
            lock (keys)
            {
                foreach (var key in keys.ToList())
                {
                    // Check if key should be invalidated based on time range
                    if (ShouldInvalidateForTimeRange(key, affectedTimeRange))
                    {
                        InvalidateCacheKey(key);
                        invalidatedKeys.Add(key);
                    }
                }
            }
        }

        _logger.LogInformation("Time series data changed for {MeasurementId}, invalidated {Count} cache entries",
            measurementId, invalidatedKeys.Count);
    }

    /// <summary>
    /// Invalidates cache entries when configuration changes affect calculations.
    /// </summary>
    public void OnConfigurationChanged(ConfigurationChangeType changeType, string? affectedComponent = null)
    {
        var invalidatedKeys = new HashSet<string>();

        foreach (var kvp in _keyMetadata.ToList())
        {
            var cacheKey = kvp.Key;
            var metadata = kvp.Value;

            var shouldInvalidate = changeType switch
            {
                ConfigurationChangeType.StatisticalConfig => 
                    metadata.CacheType.Contains("correlation") || metadata.CacheType.Contains("statistical"),
                    
                ConfigurationChangeType.OptimizationConfig => 
                    metadata.CacheType.Contains("optimization") || metadata.CacheType.Contains("boundary"),
                    
                ConfigurationChangeType.ValidationConfig => 
                    metadata.CacheType.Contains("validation") || metadata.CacheType.Contains("walkforward"),
                    
                ConfigurationChangeType.CachingConfig => true, // Invalidate everything
                
                ConfigurationChangeType.ComponentSpecific when !string.IsNullOrEmpty(affectedComponent) =>
                    metadata.CacheType.Contains(affectedComponent, StringComparison.OrdinalIgnoreCase),
                    
                _ => false
            };

            if (shouldInvalidate)
            {
                InvalidateCacheKey(cacheKey);
                invalidatedKeys.Add(cacheKey);
            }
        }

        _logger.LogInformation("Configuration changed ({ChangeType}), invalidated {Count} cache entries",
            changeType, invalidatedKeys.Count);
    }

    /// <summary>
    /// Performs bulk invalidation of cache entries based on patterns.
    /// </summary>
    public void InvalidateByPattern(string pattern, InvalidationScope scope = InvalidationScope.Exact)
    {
        var invalidatedKeys = new HashSet<string>();

        foreach (var cacheKey in _keyMetadata.Keys.ToList())
        {
            var shouldInvalidate = scope switch
            {
                InvalidationScope.Exact => cacheKey == pattern,
                InvalidationScope.StartsWith => cacheKey.StartsWith(pattern),
                InvalidationScope.Contains => cacheKey.Contains(pattern),
                InvalidationScope.EndsWith => cacheKey.EndsWith(pattern),
                _ => false
            };

            if (shouldInvalidate)
            {
                InvalidateCacheKey(cacheKey);
                invalidatedKeys.Add(cacheKey);
            }
        }

        _logger.LogInformation("Pattern invalidation '{Pattern}' ({Scope}), invalidated {Count} cache entries",
            pattern, scope, invalidatedKeys.Count);
    }

    /// <summary>
    /// Invalidates cache entries older than the specified age.
    /// </summary>
    public void InvalidateByAge(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
        var invalidatedKeys = new HashSet<string>();

        foreach (var kvp in _keyCreationTimes.ToList())
        {
            if (kvp.Value < cutoffTime)
            {
                InvalidateCacheKey(kvp.Key);
                invalidatedKeys.Add(kvp.Key);
            }
        }

        _logger.LogInformation("Age-based invalidation (older than {MaxAge}), invalidated {Count} cache entries",
            maxAge, invalidatedKeys.Count);
    }

    /// <summary>
    /// Gets invalidation statistics for monitoring.
    /// </summary>
    public CacheInvalidationStatistics GetStatistics()
    {
        var totalKeys = _keyMetadata.Count;
        var keysByType = _keyMetadata.Values
            .GroupBy(m => m.CacheType)
            .ToDictionary(g => g.Key, g => g.Count());

        var oldestKey = _keyCreationTimes.Values.Any() 
            ? _keyCreationTimes.Values.Min() 
            : DateTime.UtcNow;

        return new CacheInvalidationStatistics
        {
            TotalTrackedKeys = totalKeys,
            KeysByType = keysByType,
            DataHashCount = _dataHashToCacheKeys.Count,
            OldestKeyAge = DateTime.UtcNow.Subtract(oldestKey),
            LastInvalidationTime = DateTime.UtcNow // Would track actual last invalidation
        };
    }

    /// <summary>
    /// Clears all invalidation tracking data.
    /// </summary>
    public void ClearTrackingData()
    {
        _dataHashToCacheKeys.Clear();
        _keyCreationTimes.Clear();
        _keyMetadata.Clear();
        
        _logger.LogInformation("Cleared all cache invalidation tracking data");
    }

    private void InvalidateCacheKey(string cacheKey)
    {
        _cache.Remove(cacheKey);
        
        // Clean up tracking data
        _keyMetadata.TryRemove(cacheKey, out _);
        _keyCreationTimes.TryRemove(cacheKey, out _);
        
        // Remove from data hash indexes
        foreach (var hashEntry in _dataHashToCacheKeys.ToList())
        {
            lock (hashEntry.Value)
            {
                hashEntry.Value.Remove(cacheKey);
                if (hashEntry.Value.Count == 0)
                {
                    _dataHashToCacheKeys.TryRemove(hashEntry.Key, out _);
                }
            }
        }
    }

    private bool ShouldInvalidateForTimeRange(string cacheKey, TimeSpan? affectedTimeRange)
    {
        if (!affectedTimeRange.HasValue)
            return true; // Invalidate everything if no specific range

        if (!_keyMetadata.TryGetValue(cacheKey, out var metadata))
            return true; // Invalidate if we can't determine metadata

        // If the cache key's time horizon overlaps with the affected range, invalidate it
        return metadata.TimeHorizon == null || 
               metadata.TimeHorizon.Value <= affectedTimeRange.Value;
    }
}

/// <summary>
/// Metadata about a cache key for tracking dependencies and invalidation rules.
/// </summary>
public class CacheKeyMetadata
{
    /// <summary>
    /// Type of cached data (correlation, optimization, validation, etc.)
    /// </summary>
    public string CacheType { get; init; } = string.Empty;
    
    /// <summary>
    /// Hash of the underlying data used in the calculation
    /// </summary>
    public string DataHash { get; init; } = string.Empty;
    
    /// <summary>
    /// Measurement ID if cache is specific to a measurement
    /// </summary>
    public string? MeasurementId { get; init; }
    
    /// <summary>
    /// Time horizon used in the calculation
    /// </summary>
    public TimeSpan? TimeHorizon { get; init; }
    
    /// <summary>
    /// Configuration hash used in the calculation
    /// </summary>
    public string? ConfigHash { get; init; }
    
    /// <summary>
    /// Additional tags for flexible invalidation strategies
    /// </summary>
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Statistics about cache invalidation operations.
/// </summary>
public class CacheInvalidationStatistics
{
    public int TotalTrackedKeys { get; init; }
    public Dictionary<string, int> KeysByType { get; init; } = new();
    public int DataHashCount { get; init; }
    public TimeSpan OldestKeyAge { get; init; }
    public DateTime LastInvalidationTime { get; init; }
}

/// <summary>
/// Types of configuration changes that can trigger cache invalidation.
/// </summary>
public enum ConfigurationChangeType
{
    StatisticalConfig,
    OptimizationConfig,
    ValidationConfig,
    CachingConfig,
    ComponentSpecific
}

/// <summary>
/// Scope for pattern-based cache invalidation.
/// </summary>
public enum InvalidationScope
{
    Exact,
    StartsWith,
    Contains,
    EndsWith
}