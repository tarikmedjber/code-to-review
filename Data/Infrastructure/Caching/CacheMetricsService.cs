using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Infrastructure.Interfaces;

namespace MedjCap.Data.Infrastructure.Caching;

/// <summary>
/// Service for collecting and reporting cache performance metrics.
/// Provides detailed insights into cache hit rates, performance, and efficiency.
/// </summary>
public class CacheMetricsService : ICacheMetricsService
{
    private readonly CachingConfig _config;
    private readonly ILogger<CacheMetricsService> _logger;

    // Concurrent collections for thread-safe metrics tracking
    private readonly ConcurrentDictionary<string, CacheOperationMetric> _operationMetrics = new();
    private readonly ConcurrentDictionary<string, CacheHitMissCounter> _hitMissCounters = new();
    private readonly ConcurrentDictionary<string, CacheTimingMetric> _timingMetrics = new();
    
    private long _totalOperations = 0;
    private long _totalHits = 0;
    private long _totalMisses = 0;
    private long _totalInvalidations = 0;

    public CacheMetricsService(
        IOptions<CachingConfig> config,
        ILogger<CacheMetricsService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records a cache operation (hit or miss) with timing information.
    /// </summary>
    public void RecordCacheOperation(string cacheType, string operation, bool wasHit, TimeSpan duration, int? dataSize = null)
    {
        Interlocked.Increment(ref _totalOperations);
        
        if (wasHit)
        {
            Interlocked.Increment(ref _totalHits);
        }
        else
        {
            Interlocked.Increment(ref _totalMisses);
        }

        // Track by cache type
        var key = $"{cacheType}:{operation}";
        
        _operationMetrics.AddOrUpdate(key, 
            new CacheOperationMetric
            {
                CacheType = cacheType,
                Operation = operation,
                TotalOperations = 1,
                Hits = wasHit ? 1 : 0,
                Misses = wasHit ? 0 : 1,
                TotalDuration = duration,
                MinDuration = duration,
                MaxDuration = duration,
                LastOperation = DateTime.UtcNow,
                AverageDataSize = dataSize ?? 0
            },
            (existingKey, existing) =>
            {
                var newMetric = new CacheOperationMetric
                {
                    CacheType = existing.CacheType,
                    Operation = existing.Operation,
                    TotalOperations = existing.TotalOperations + 1,
                    Hits = existing.Hits + (wasHit ? 1 : 0),
                    Misses = existing.Misses + (wasHit ? 0 : 1),
                    TotalDuration = existing.TotalDuration.Add(duration),
                    MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration,
                    MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration,
                    LastOperation = DateTime.UtcNow,
                    AverageDataSize = dataSize != null 
                        ? (existing.AverageDataSize * existing.TotalOperations + dataSize.Value) / (existing.TotalOperations + 1)
                        : existing.AverageDataSize
                };
                return newMetric;
            });

        // Track hit/miss counters for quick access
        _hitMissCounters.AddOrUpdate(cacheType,
            new CacheHitMissCounter { Hits = wasHit ? 1 : 0, Misses = wasHit ? 0 : 1 },
            (existingKey, existing) => new CacheHitMissCounter
            {
                Hits = existing.Hits + (wasHit ? 1 : 0),
                Misses = existing.Misses + (wasHit ? 0 : 1)
            });

        // Track timing metrics
        _timingMetrics.AddOrUpdate(operation,
            new CacheTimingMetric
            {
                Operation = operation,
                Count = 1,
                TotalDuration = duration,
                MinDuration = duration,
                MaxDuration = duration
            },
            (existingKey, existing) => new CacheTimingMetric
            {
                Operation = existing.Operation,
                Count = existing.Count + 1,
                TotalDuration = existing.TotalDuration.Add(duration),
                MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration,
                MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration
            });

        // Log performance concerns
        if (duration > TimeSpan.FromMilliseconds(100))
        {
            _logger.LogWarning("Slow cache operation detected: {CacheType}:{Operation} took {Duration}ms",
                cacheType, operation, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Records a cache invalidation event.
    /// </summary>
    public void RecordCacheInvalidation(string cacheType, int keysInvalidated, string? reason = null)
    {
        Interlocked.Increment(ref _totalInvalidations);

        var invalidationKey = $"invalidation:{cacheType}";
        _operationMetrics.AddOrUpdate(invalidationKey,
            new CacheOperationMetric
            {
                CacheType = cacheType,
                Operation = "invalidation",
                TotalOperations = 1,
                Hits = 0,
                Misses = 0,
                TotalDuration = TimeSpan.Zero,
                MinDuration = TimeSpan.Zero,
                MaxDuration = TimeSpan.Zero,
                LastOperation = DateTime.UtcNow,
                AverageDataSize = keysInvalidated
            },
            (existingKey, existing) => new CacheOperationMetric
            {
                CacheType = existing.CacheType,
                Operation = existing.Operation,
                TotalOperations = existing.TotalOperations + 1,
                Hits = existing.Hits,
                Misses = existing.Misses,
                TotalDuration = existing.TotalDuration,
                MinDuration = existing.MinDuration,
                MaxDuration = existing.MaxDuration,
                LastOperation = DateTime.UtcNow,
                AverageDataSize = (existing.AverageDataSize * existing.TotalOperations + keysInvalidated) / (existing.TotalOperations + 1)
            });

        _logger.LogInformation("Cache invalidation: {CacheType}, {Count} keys invalidated, reason: {Reason}",
            cacheType, keysInvalidated, reason ?? "unspecified");
    }

    /// <summary>
    /// Gets comprehensive cache performance metrics.
    /// </summary>
    public CachePerformanceMetrics GetPerformanceMetrics()
    {
        var totalOps = _totalOperations;
        var hitRate = totalOps > 0 ? (double)_totalHits / totalOps : 0.0;
        
        return new CachePerformanceMetrics
        {
            TotalOperations = totalOps,
            TotalHits = _totalHits,
            TotalMisses = _totalMisses,
            TotalInvalidations = _totalInvalidations,
            OverallHitRate = hitRate,
            OverallMissRate = 1.0 - hitRate,
            MetricsCollectedAt = DateTime.UtcNow,
            
            // Detailed metrics by cache type
            OperationMetrics = _operationMetrics.Values.ToList(),
            HitRatesByType = _hitMissCounters.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Hits + kvp.Value.Misses > 0 
                    ? (double)kvp.Value.Hits / (kvp.Value.Hits + kvp.Value.Misses)
                    : 0.0),
            
            // Performance insights
            AverageOperationDuration = CalculateAverageOperationDuration(),
            SlowestOperations = GetSlowestOperations(5),
            MostActiveOperations = GetMostActiveOperations(5),
            
            // Health indicators
            PerformanceScore = CalculatePerformanceScore(hitRate),
            RecommendedActions = GenerateRecommendations(hitRate)
        };
    }

    /// <summary>
    /// Gets metrics for a specific cache type.
    /// </summary>
    public CacheTypeMetrics? GetMetricsForCacheType(string cacheType)
    {
        if (!_hitMissCounters.TryGetValue(cacheType, out var counter))
            return null;

        var totalOps = counter.Hits + counter.Misses;
        var hitRate = totalOps > 0 ? (double)counter.Hits / totalOps : 0.0;

        var relatedOperations = _operationMetrics.Values
            .Where(m => m.CacheType == cacheType)
            .ToList();

        return new CacheTypeMetrics
        {
            CacheType = cacheType,
            TotalOperations = totalOps,
            Hits = counter.Hits,
            Misses = counter.Misses,
            HitRate = hitRate,
            AverageOperationDuration = relatedOperations.Any() 
                ? TimeSpan.FromTicks((long)relatedOperations.Average(op => 
                    op.TotalOperations > 0 ? op.TotalDuration.Ticks / (double)op.TotalOperations : 0))
                : TimeSpan.Zero,
            Operations = relatedOperations
        };
    }

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    public void ResetMetrics()
    {
        _operationMetrics.Clear();
        _hitMissCounters.Clear();
        _timingMetrics.Clear();
        
        Interlocked.Exchange(ref _totalOperations, 0);
        Interlocked.Exchange(ref _totalHits, 0);
        Interlocked.Exchange(ref _totalMisses, 0);
        Interlocked.Exchange(ref _totalInvalidations, 0);

        _logger.LogInformation("Cache metrics have been reset");
    }

    /// <summary>
    /// Starts periodic logging of cache metrics for monitoring.
    /// </summary>
    public Task StartPeriodicReporting(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken);
                    
                    var metrics = GetPerformanceMetrics();
                    _logger.LogInformation(
                        "Cache Performance Report - Hit Rate: {HitRate:P2}, Total Ops: {TotalOps}, Score: {Score}/100",
                        metrics.OverallHitRate, metrics.TotalOperations, metrics.PerformanceScore);
                    
                    // Log top performers and problem areas
                    var topHitRates = metrics.HitRatesByType
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(3);
                    
                    foreach (var (cacheType, hitRate) in topHitRates)
                    {
                        _logger.LogDebug("Cache type {CacheType}: {HitRate:P2} hit rate", cacheType, hitRate);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache metrics reporting");
                }
            }
        }, cancellationToken);
    }

    private TimeSpan CalculateAverageOperationDuration()
    {
        var allTimings = _timingMetrics.Values;
        if (!allTimings.Any())
            return TimeSpan.Zero;

        var totalTicks = allTimings.Sum(t => t.TotalDuration.Ticks);
        var totalCount = allTimings.Sum(t => (long)t.Count);
        
        return totalCount > 0 ? TimeSpan.FromTicks(totalTicks / totalCount) : TimeSpan.Zero;
    }

    private List<CacheOperationMetric> GetSlowestOperations(int count)
    {
        return _operationMetrics.Values
            .Where(m => m.TotalOperations > 0)
            .OrderByDescending(m => m.TotalDuration.TotalMilliseconds / m.TotalOperations)
            .Take(count)
            .ToList();
    }

    private List<CacheOperationMetric> GetMostActiveOperations(int count)
    {
        return _operationMetrics.Values
            .OrderByDescending(m => m.TotalOperations)
            .Take(count)
            .ToList();
    }

    private int CalculatePerformanceScore(double hitRate)
    {
        var score = 0;
        
        // Hit rate score (0-50 points)
        score += (int)(hitRate * 50);
        
        // Consistency score (0-25 points) - based on hit rate variance
        var hitRateVariance = CalculateHitRateVariance();
        score += Math.Max(0, 25 - (int)(hitRateVariance * 100));
        
        // Performance score (0-25 points) - based on average response time
        var avgDuration = CalculateAverageOperationDuration();
        if (avgDuration.TotalMilliseconds < 10)
            score += 25;
        else if (avgDuration.TotalMilliseconds < 50)
            score += 15;
        else if (avgDuration.TotalMilliseconds < 100)
            score += 10;
        
        return Math.Min(100, score);
    }

    private double CalculateHitRateVariance()
    {
        var hitRates = _hitMissCounters.Values
            .Where(counter => counter.Hits + counter.Misses > 0)
            .Select(counter => (double)counter.Hits / (counter.Hits + counter.Misses))
            .ToList();
        
        if (!hitRates.Any())
            return 0.0;
        
        var average = hitRates.Average();
        var variance = hitRates.Sum(rate => Math.Pow(rate - average, 2)) / hitRates.Count;
        return Math.Sqrt(variance);
    }

    private List<string> GenerateRecommendations(double overallHitRate)
    {
        var recommendations = new List<string>();
        
        if (overallHitRate < 0.3)
        {
            recommendations.Add("Consider increasing cache TTL values - hit rate is very low");
            recommendations.Add("Review data access patterns - frequent cache misses detected");
        }
        else if (overallHitRate < 0.5)
        {
            recommendations.Add("Consider optimizing cache key generation for better locality");
        }
        
        var avgDuration = CalculateAverageOperationDuration();
        if (avgDuration.TotalMilliseconds > 100)
        {
            recommendations.Add("Cache operations are slow - consider cache size limits or faster storage");
        }
        
        if (_totalInvalidations > _totalHits * 0.1)
        {
            recommendations.Add("High invalidation rate detected - review invalidation strategy");
        }
        
        return recommendations;
    }
}

/// <summary>
/// Hit/miss counter for efficient tracking.
/// </summary>
internal class CacheHitMissCounter
{
    public long Hits { get; set; }
    public long Misses { get; set; }
}

/// <summary>
/// Timing metrics for cache operations.
/// </summary>
internal class CacheTimingMetric
{
    public string Operation { get; set; } = string.Empty;
    public long Count { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
}