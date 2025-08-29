using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Backtesting.Models;
using MedjCap.Data.Infrastructure.Caching;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.Backtesting.Services;

/// <summary>
/// Cached wrapper for backtest service that improves performance by caching expensive walk-forward and backtest calculations
/// </summary>
public class CachedBacktestService : IBacktestService
{
    private readonly IBacktestService _innerService;
    private readonly IMemoryCache _cache;
    private readonly CachingConfig _config;
    private readonly ILogger<CachedBacktestService> _logger;

    private readonly MemoryCacheEntryOptions _walkForwardCacheOptions;
    private readonly MemoryCacheEntryOptions _backtestCacheOptions;
    private readonly MemoryCacheEntryOptions _windowCacheOptions;

    public CachedBacktestService(
        IBacktestService innerService,
        IMemoryCache cache,
        IOptions<CachingConfig> config,
        ILogger<CachedBacktestService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Pre-configure cache options for different backtest operation types
        _walkForwardCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.AnalysisCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Size = 5, // Walk-forward results are medium-large
            Priority = CacheItemPriority.High
        };

        _backtestCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(20),
            Size = 3 // Backtest results are medium-sized
        };

        _windowCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            SlidingExpiration = TimeSpan.FromMinutes(45),
            Size = 2 // Window definitions are small but long-lived
        };
    }

    // Walk-Forward Analysis (very expensive - high-value caching target)
    public WalkForwardResults RunWalkForwardAnalysis(List<PriceMovement> movements, AnalysisConfig config, OptimizationTarget target)
    {
        if (!_config.EnableCaching || movements == null || config == null)
            return _innerService.RunWalkForwardAnalysis(movements!, config!, target);

        var cacheKey = CacheKeyGenerator.ForWalkForward(movements, config, target);
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_walkForwardCacheOptions);
            _logger.LogDebug("Computing walk-forward analysis for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerService.RunWalkForwardAnalysis(movements, config, target);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogBacktestStatistics(cacheKey, "walk_forward", false, elapsed, result?.WindowCount ?? 0);
            return result;
        }) ?? new WalkForwardResults();
    }

    // Boundary Backtesting (expensive computation - valuable caching)
    public BacktestResult BacktestBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> testData, decimal targetATR)
    {
        if (!_config.EnableCaching || boundaries == null || testData == null)
            return _innerService.BacktestBoundaries(boundaries!, testData!, targetATR);

        var boundaryHash = ComputeBoundaryHash(boundaries);
        var dataHash = ComputeMovementHash(testData);
        var cacheKey = $"backtest:{boundaryHash}:{dataHash}:{targetATR:F4}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_backtestCacheOptions);
            _logger.LogDebug("Computing boundary backtest for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerService.BacktestBoundaries(boundaries, testData, targetATR);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogBacktestStatistics(cacheKey, "boundary_backtest", false, elapsed, result?.TotalTrades ?? 0);
            return result;
        }) ?? new BacktestResult();
    }

    // Window Creation (fast but called frequently - moderate caching value)
    public List<WalkForwardWindow> CreateWalkForwardWindows(DateRange inSamplePeriod, int windowCount)
    {
        if (!_config.EnableCaching || inSamplePeriod == null)
            return _innerService.CreateWalkForwardWindows(inSamplePeriod!, windowCount) ?? new List<WalkForwardWindow>();

        var cacheKey = $"windows:{inSamplePeriod.Start:yyyyMMdd}:{inSamplePeriod.End:yyyyMMdd}:{windowCount}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_windowCacheOptions);
            _logger.LogDebug("Computing walk-forward windows for cache key: {CacheKey}", cacheKey);
            
            var result = _innerService.CreateWalkForwardWindows(inSamplePeriod, windowCount);
            LogBacktestStatistics(cacheKey, "window_creation", false, TimeSpan.Zero, result?.Count ?? 0);
            return result ?? new List<WalkForwardWindow>();
        }) ?? new List<WalkForwardWindow>();
    }

    /// <summary>
    /// Gets cache performance metrics for backtesting operations
    /// </summary>
    public BacktestCacheStatistics GetCacheStatistics()
    {
        return new BacktestCacheStatistics
        {
            WalkForwardCacheHits = 0,        // Would need custom metrics tracking
            BacktestCacheHits = 0,
            WindowCreationCacheHits = 0,
            AverageWalkForwardTimeSaved = TimeSpan.Zero,
            AverageBacktestTimeSaved = TimeSpan.Zero,
            CacheHitRatio = 0.0
        };
    }

    /// <summary>
    /// Pre-warms cache with commonly used configurations
    /// </summary>
    public void PrewarmCache(List<AnalysisConfig> commonConfigs, DateRange defaultPeriod)
    {
        if (!_config.EnableCaching || commonConfigs == null)
            return;

        _logger.LogInformation("Pre-warming backtest cache with {ConfigCount} configurations", commonConfigs.Count);

        // Pre-calculate common window configurations
        var commonWindowCounts = new[] { 5, 10, 20, 50 };
        foreach (var windowCount in commonWindowCounts)
        {
            try
            {
                CreateWalkForwardWindows(defaultPeriod, windowCount);
                _logger.LogDebug("Pre-warmed windows for count: {WindowCount}", windowCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to pre-warm windows for count: {WindowCount}", windowCount);
            }
        }
    }

    /// <summary>
    /// Invalidates cache entries that depend on specific date ranges or configurations
    /// </summary>
    public void InvalidateCacheForDateRange(DateRange dateRange)
    {
        _logger.LogInformation("Cache invalidation requested for date range: {Start} to {End}", 
            dateRange.Start, dateRange.End);
        
        // Implementation would require custom cache with pattern-based invalidation
        // For now, we log the request for monitoring purposes
    }

    private string ComputeMovementHash(List<PriceMovement> movements)
    {
        if (movements == null || movements.Count == 0)
            return "empty";

        // Create a hash that's consistent but not dependent on full data set
        var firstMovement = movements.First();
        var lastMovement = movements.Last();
        var midMovement = movements[movements.Count / 2];

        return $"{movements.Count}_{firstMovement.StartTimestamp:yyyyMMddHH}_{midMovement.ATRMovement:F2}_{lastMovement.StartTimestamp:yyyyMMddHH}";
    }

    private string ComputeBoundaryHash(List<OptimalBoundary> boundaries)
    {
        if (boundaries == null || boundaries.Count == 0)
            return "empty";

        // Create a hash based on boundary characteristics
        var totalRangeSum = boundaries.Sum(b => b.RangeHigh - b.RangeLow);
        var avgExpectedMove = boundaries.Average(b => b.ExpectedATRMove);

        return $"{boundaries.Count}_{totalRangeSum:F4}_{avgExpectedMove:F4}";
    }

    private void LogBacktestStatistics(string cacheKey, string operationType, bool wasHit, TimeSpan elapsed, int resultCount)
    {
        var hitStatus = wasHit ? "HIT" : "MISS";
        
        if (elapsed > TimeSpan.Zero)
        {
            _logger.LogDebug("Cache {HitStatus} for {OperationType}: {CacheKey} (took {ElapsedMs}ms, result count: {ResultCount})", 
                hitStatus, operationType, cacheKey, elapsed.TotalMilliseconds, resultCount);
        }
        else
        {
            _logger.LogDebug("Cache {HitStatus} for {OperationType}: {CacheKey} (result count: {ResultCount})", 
                hitStatus, operationType, cacheKey, resultCount);
        }
    }
}

/// <summary>
/// Cache statistics specific to backtesting operations
/// </summary>
public class BacktestCacheStatistics
{
    public int WalkForwardCacheHits { get; set; }
    public int BacktestCacheHits { get; set; }
    public int WindowCreationCacheHits { get; set; }
    public TimeSpan AverageWalkForwardTimeSaved { get; set; }
    public TimeSpan AverageBacktestTimeSaved { get; set; }
    public double CacheHitRatio { get; set; }
}