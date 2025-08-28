using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Services;

/// <summary>
/// Cached wrapper for correlation service that improves performance by caching expensive calculations
/// </summary>
public class CachedCorrelationService : ICorrelationService
{
    private readonly ICorrelationService _innerService;
    private readonly IMemoryCache _cache;
    private readonly CachingConfig _config;
    private readonly ILogger<CachedCorrelationService> _logger;
    private readonly ICacheInvalidationService? _invalidationService;
    private readonly ICacheMetricsService? _metricsService;

    private readonly MemoryCacheEntryOptions _correlationCacheOptions;
    private readonly MemoryCacheEntryOptions _statisticalCacheOptions;
    private readonly MemoryCacheEntryOptions _analysisCacheOptions;

    public CachedCorrelationService(
        ICorrelationService innerService,
        IMemoryCache cache,
        IOptions<CachingConfig> config,
        ILogger<CachedCorrelationService> logger,
        ICacheInvalidationService? invalidationService = null,
        ICacheMetricsService? metricsService = null)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _invalidationService = invalidationService;
        _metricsService = metricsService;

        // Pre-configure cache options for different operation types
        _correlationCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.CorrelationCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Size = 1 // Each correlation result counts as 1 unit
        };

        _statisticalCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.StatisticalCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Size = 1
        };

        _analysisCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.AnalysisCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(20),
            Size = 5 // Analysis results are larger
        };
    }

    // Price Movement Calculation (caching with data hash)
    public List<PriceMovement> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan timeHorizon)
    {
        if (!_config.EnableCaching || timeSeries == null)
            return _innerService.CalculatePriceMovements(timeSeries!, timeHorizon) ?? new List<PriceMovement>();

        var cacheKey = $"pricemovements:{ComputeTimeSeriesHash(timeSeries)}:{timeHorizon}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_correlationCacheOptions);
            _logger.LogDebug("Computing price movements for cache key: {CacheKey}", cacheKey);
            
            // Register cache key for invalidation tracking
            RegisterCacheKeyForInvalidation(cacheKey, "price_movements", timeSeries, timeHorizon);
            
            return _innerService.CalculatePriceMovements(timeSeries, timeHorizon) ?? new List<PriceMovement>();
        }) ?? new List<PriceMovement>();
    }

    public Dictionary<TimeSpan, List<PriceMovement>> CalculatePriceMovements(TimeSeriesData timeSeries, TimeSpan[] timeHorizons)
    {
        if (!_config.EnableCaching || timeSeries == null || timeHorizons == null)
            return _innerService.CalculatePriceMovements(timeSeries!, timeHorizons!) ?? new Dictionary<TimeSpan, List<PriceMovement>>();

        var cacheKey = $"pricemovements_multi:{ComputeTimeSeriesHash(timeSeries)}:{string.Join(",", timeHorizons)}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_correlationCacheOptions);
            _logger.LogDebug("Computing multi-horizon price movements for cache key: {CacheKey}", cacheKey);
            return _innerService.CalculatePriceMovements(timeSeries, timeHorizons) ?? new Dictionary<TimeSpan, List<PriceMovement>>();
        }) ?? new Dictionary<TimeSpan, List<PriceMovement>>();
    }

    // Correlation Analysis (primary caching target)
    public CorrelationResult CalculateCorrelation(List<PriceMovement> movements, CorrelationType correlationType)
    {
        if (!_config.EnableCaching || movements == null)
            return _innerService.CalculateCorrelation(movements!, correlationType);

        var cacheKey = CacheKeyGenerator.ForCorrelation(movements, correlationType);
        
        return GetOrCreateWithMetrics(cacheKey, "correlation", "CalculateCorrelation", 
            _correlationCacheOptions, () =>
            {
                // Register cache key for invalidation tracking
                RegisterCacheKeyForInvalidation(cacheKey, "correlation", movements);
                return _innerService.CalculateCorrelation(movements, correlationType);
            }) ?? new CorrelationResult();
    }

    // Data Analysis & Segmentation (caching bucketized results)
    public Dictionary<string, List<PriceMovement>> BucketizeMovements(List<PriceMovement> movements, decimal[] atrTargets)
    {
        if (!_config.EnableCaching || movements == null || atrTargets == null)
            return _innerService.BucketizeMovements(movements!, atrTargets!) ?? new Dictionary<string, List<PriceMovement>>();

        var dataHash = ComputeMovementHash(movements);
        var targetHash = string.Join(",", atrTargets);
        var cacheKey = $"bucketize:{dataHash}:{targetHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_correlationCacheOptions);
            _logger.LogDebug("Computing bucketized movements for cache key: {CacheKey}", cacheKey);
            return _innerService.BucketizeMovements(movements, atrTargets) ?? new Dictionary<string, List<PriceMovement>>();
        }) ?? new Dictionary<string, List<PriceMovement>>();
    }

    public Dictionary<string, RangeAnalysisResult> AnalyzeByMeasurementRanges(List<PriceMovement> movements, List<(decimal Low, decimal High)> measurementRanges)
    {
        if (!_config.EnableCaching || movements == null || measurementRanges == null)
            return _innerService.AnalyzeByMeasurementRanges(movements!, measurementRanges!) ?? new Dictionary<string, RangeAnalysisResult>();

        var dataHash = ComputeMovementHash(movements);
        var rangeHash = string.Join(",", measurementRanges.Select(r => $"{r.Low}-{r.High}"));
        var cacheKey = $"rangeanalysis:{dataHash}:{rangeHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_analysisCacheOptions);
            _logger.LogDebug("Computing range analysis for cache key: {CacheKey}", cacheKey);
            return _innerService.AnalyzeByMeasurementRanges(movements, measurementRanges) ?? new Dictionary<string, RangeAnalysisResult>();
        }) ?? new Dictionary<string, RangeAnalysisResult>();
    }

    // Contextual Filtering (caching with context parameters)
    public CorrelationResult CalculateWithContextualFilter(List<PriceMovement> movements, string contextVariable, decimal contextThreshold, ComparisonOperator comparisonOperator)
    {
        if (!_config.EnableCaching || movements == null || contextVariable == null)
            return _innerService.CalculateWithContextualFilter(movements!, contextVariable!, contextThreshold, comparisonOperator);

        var dataHash = ComputeMovementHash(movements);
        var cacheKey = $"contextual:{dataHash}:{contextVariable}:{contextThreshold}:{comparisonOperator}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_correlationCacheOptions);
            _logger.LogDebug("Computing contextual correlation for cache key: {CacheKey}", cacheKey);
            return _innerService.CalculateWithContextualFilter(movements, contextVariable, contextThreshold, comparisonOperator);
        }) ?? new CorrelationResult();
    }

    // Comprehensive Analysis (highest value caching target)
    public CorrelationAnalysisResult RunFullAnalysis(TimeSeriesData timeSeries, CorrelationAnalysisRequest request)
    {
        if (!_config.EnableCaching || timeSeries == null || request == null)
            return _innerService.RunFullAnalysis(timeSeries!, request!);

        var timeSeriesHash = ComputeTimeSeriesHash(timeSeries);
        var requestHash = ComputeRequestHash(request);
        var cacheKey = $"fullanalysis:{timeSeriesHash}:{requestHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_analysisCacheOptions);
            _logger.LogDebug("Computing full analysis for cache key: {CacheKey}", cacheKey);
            
            var result = _innerService.RunFullAnalysis(timeSeries, request);
            LogCacheHit(cacheKey, "full_analysis", false);
            return result;
        }) ?? new CorrelationAnalysisResult();
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        // Note: MemoryCache doesn't expose detailed statistics by default
        // This would need to be enhanced with custom cache metrics tracking
        return new CacheStatistics
        {
            TotalEntries = 0, // Would need custom tracking
            HitRate = 0.0,   // Would need custom tracking
            MissRate = 0.0,  // Would need custom tracking
            CacheSize = 0    // Would need custom tracking
        };
    }

    /// <summary>
    /// Clears all cached results (useful for testing and maintenance)
    /// </summary>
    public void ClearCache()
    {
        // MemoryCache doesn't have a public Clear method
        // This would require implementing a custom cache with clear capability
        _logger.LogWarning("Cache clear requested but MemoryCache doesn't support bulk clear operations");
    }

    private string ComputeTimeSeriesHash(TimeSeriesData timeSeries)
    {
        if (timeSeries?.DataPoints == null)
            return "empty";

        var dataPointsList = timeSeries.DataPoints.ToList();
        if (dataPointsList.Count == 0)
            return "empty";

        // Create a hash based on key characteristics without full data dependency
        return $"{dataPointsList.Count}:{dataPointsList.First().Timestamp:yyyyMMddHHmm}:{dataPointsList.Last().Timestamp:yyyyMMddHHmm}";
    }

    private string ComputeMovementHash(List<PriceMovement> movements)
    {
        if (movements == null || movements.Count == 0)
            return "empty";

        return $"{movements.Count}:{movements.First().StartTimestamp:yyyyMMddHHmm}:{movements.Last().StartTimestamp:yyyyMMddHHmm}";
    }

    private string ComputeRequestHash(CorrelationAnalysisRequest request)
    {
        if (request == null)
            return "default";

        return $"{request.MeasurementId}:{request.TimeHorizons?.Length ?? 0}:{request.ATRTargets?.Length ?? 0}";
    }

    private void LogCacheHit(string cacheKey, string operationType, bool wasHit)
    {
        if (wasHit)
            _logger.LogDebug("Cache HIT for {OperationType}: {CacheKey}", operationType, cacheKey);
        else
            _logger.LogDebug("Cache MISS for {OperationType}: {CacheKey}", operationType, cacheKey);
    }

    private void RegisterCacheKeyForInvalidation(string cacheKey, string cacheType, List<PriceMovement> movements, string? measurementId = null)
    {
        if (_invalidationService == null)
            return;

        var dataHash = ComputeMovementHash(movements);
        var metadata = new CacheKeyMetadata
        {
            CacheType = cacheType,
            DataHash = dataHash,
            MeasurementId = measurementId ?? ExtractMeasurementIdFromMovements(movements)
        };

        _invalidationService.RegisterCacheKey(cacheKey, metadata);
    }

    private void RegisterCacheKeyForInvalidation(string cacheKey, string cacheType, TimeSeriesData timeSeries, TimeSpan? timeHorizon = null)
    {
        if (_invalidationService == null)
            return;

        var dataHash = ComputeTimeSeriesHash(timeSeries);
        var metadata = new CacheKeyMetadata
        {
            CacheType = cacheType,
            DataHash = dataHash,
            MeasurementId = ExtractMeasurementIdFromTimeSeries(timeSeries),
            TimeHorizon = timeHorizon
        };

        _invalidationService.RegisterCacheKey(cacheKey, metadata);
    }

    private string? ExtractMeasurementIdFromMovements(List<PriceMovement> movements)
    {
        // PriceMovement doesn't contain MeasurementId, return null
        // The measurement ID should be passed explicitly when available
        return null;
    }

    private string? ExtractMeasurementIdFromTimeSeries(TimeSeriesData timeSeries)
    {
        // Extract measurement ID from time series data if available
        return timeSeries?.DataPoints?.FirstOrDefault()?.MeasurementId;
    }

    /// <summary>
    /// Generic method to get or create cache entries with metrics collection.
    /// </summary>
    private T? GetOrCreateWithMetrics<T>(
        string cacheKey, 
        string cacheType, 
        string operation, 
        MemoryCacheEntryOptions cacheOptions, 
        Func<T> factory) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool wasHit = false;

        try
        {
            var result = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SetOptions(cacheOptions);
                _logger.LogDebug("Computing {Operation} for cache key: {CacheKey}", operation, cacheKey);
                return factory();
            });

            // Check if it was a cache hit by seeing if the factory was called
            wasHit = result != null && stopwatch.ElapsedMilliseconds < 5; // Assume < 5ms means cache hit
            
            return result;
        }
        finally
        {
            stopwatch.Stop();
            
            // Record metrics
            _metricsService?.RecordCacheOperation(
                cacheType, 
                operation, 
                wasHit, 
                stopwatch.Elapsed);

            LogCacheHit(cacheKey, operation, wasHit);
        }
    }
}

/// <summary>
/// Cache statistics for monitoring cache performance
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public long CacheSize { get; set; }
}