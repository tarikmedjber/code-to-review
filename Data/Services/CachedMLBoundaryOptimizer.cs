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
/// Cached wrapper for ML boundary optimizer that improves performance by caching expensive optimization calculations
/// </summary>
public class CachedMLBoundaryOptimizer : IMLBoundaryOptimizer
{
    private readonly IMLBoundaryOptimizer _innerOptimizer;
    private readonly IMemoryCache _cache;
    private readonly CachingConfig _config;
    private readonly ILogger<CachedMLBoundaryOptimizer> _logger;

    private readonly MemoryCacheEntryOptions _optimizationCacheOptions;
    private readonly MemoryCacheEntryOptions _validationCacheOptions;
    private readonly MemoryCacheEntryOptions _combinedCacheOptions;
    private readonly MemoryCacheEntryOptions _crossValidationCacheOptions;

    public CachedMLBoundaryOptimizer(
        IMLBoundaryOptimizer innerOptimizer,
        IMemoryCache cache,
        IOptions<CachingConfig> config,
        ILogger<CachedMLBoundaryOptimizer> logger)
    {
        _innerOptimizer = innerOptimizer ?? throw new ArgumentNullException(nameof(innerOptimizer));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Pre-configure cache options for different optimization types
        _optimizationCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.OptimizationCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Size = 3, // Optimization results are medium-sized
            Priority = CacheItemPriority.High // Keep optimization results longer
        };

        _validationCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(20),
            Size = 2
        };

        _combinedCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.OptimizationCache.TTL,
            SlidingExpiration = TimeSpan.FromMinutes(45),
            Size = 10, // Combined results are largest
            Priority = CacheItemPriority.High
        };

        _crossValidationCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2), // Cross-validation is very expensive, cache longer
            SlidingExpiration = TimeSpan.FromHours(1),
            Size = 5, // Cross-validation results are medium-sized but very valuable
            Priority = CacheItemPriority.High // Keep cross-validation results as they're very expensive to compute
        };
    }

    // Basic Boundary Optimization (high-value caching target)
    public List<OptimalBoundary> FindOptimalBoundaries(List<PriceMovement> movements, decimal targetATRMove, int maxRanges)
    {
        if (!_config.EnableCaching || movements == null)
            return _innerOptimizer.FindOptimalBoundaries(movements!, targetATRMove, maxRanges) ?? new List<OptimalBoundary>();

        var cacheKey = $"boundaries:{ComputeMovementHash(movements)}:{targetATRMove:F4}:{maxRanges}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_optimizationCacheOptions);
            _logger.LogDebug("Computing optimal boundaries for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.FindOptimalBoundaries(movements, targetATRMove, maxRanges);
            LogCacheStatistics(cacheKey, "boundaries", false, result?.Count ?? 0);
            return result ?? new List<OptimalBoundary>();
        }) ?? new List<OptimalBoundary>();
    }

    // Algorithmic Approaches (expensive computations - prime caching candidates)
    public List<decimal> OptimizeWithDecisionTree(List<PriceMovement> movements, int maxDepth)
    {
        if (!_config.EnableCaching || movements == null)
            return _innerOptimizer.OptimizeWithDecisionTree(movements!, maxDepth) ?? new List<decimal>();

        var cacheKey = $"decisiontree:{ComputeMovementHash(movements)}:{maxDepth}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_optimizationCacheOptions);
            _logger.LogDebug("Computing decision tree optimization for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.OptimizeWithDecisionTree(movements, maxDepth);
            LogCacheStatistics(cacheKey, "decision_tree", false, result?.Count ?? 0);
            return result ?? new List<decimal>();
        }) ?? new List<decimal>();
    }

    public List<ClusterResult> OptimizeWithClustering(List<PriceMovement> movements, int numberOfClusters)
    {
        if (!_config.EnableCaching || movements == null)
            return _innerOptimizer.OptimizeWithClustering(movements!, numberOfClusters) ?? new List<ClusterResult>();

        var cacheKey = $"clustering:{ComputeMovementHash(movements)}:{numberOfClusters}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_optimizationCacheOptions);
            _logger.LogDebug("Computing clustering optimization for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.OptimizeWithClustering(movements, numberOfClusters);
            LogCacheStatistics(cacheKey, "clustering", false, result?.Count ?? 0);
            return result ?? new List<ClusterResult>();
        }) ?? new List<ClusterResult>();
    }

    public OptimalRange OptimizeWithGradientSearch(List<PriceMovement> movements, OptimizationObjective objective)
    {
        if (!_config.EnableCaching || movements == null || objective == null)
            return _innerOptimizer.OptimizeWithGradientSearch(movements!, objective!);

        var cacheKey = $"gradient:{ComputeMovementHash(movements)}:{ComputeObjectiveHash(objective)}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_optimizationCacheOptions);
            _logger.LogDebug("Computing gradient search optimization for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.OptimizeWithGradientSearch(movements, objective);
            LogCacheStatistics(cacheKey, "gradient_search", false, 1);
            return result;
        }) ?? new OptimalRange();
    }

    // Advanced Optimization (highest computation cost - most valuable caching)
    public CombinedOptimizationResult RunCombinedOptimization(List<PriceMovement> movements, MLOptimizationConfig config)
    {
        if (!_config.EnableCaching || movements == null || config == null)
            return _innerOptimizer.RunCombinedOptimization(movements!, config!);

        var configHash = ComputeMLConfigHash(config);
        var cacheKey = $"combined:{ComputeMovementHash(movements)}:{configHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_combinedCacheOptions);
            _logger.LogDebug("Computing combined optimization for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.RunCombinedOptimization(movements, config);
            LogCacheStatistics(cacheKey, "combined_optimization", false, result?.OptimalBoundaries?.Count ?? 0);
            return result;
        }) ?? new CombinedOptimizationResult();
    }

    public ValidationResult ValidateBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> testMovements)
    {
        if (!_config.EnableCaching || boundaries == null || testMovements == null)
            return _innerOptimizer.ValidateBoundaries(boundaries!, testMovements!);

        var boundaryHash = ComputeBoundaryHash(boundaries);
        var movementHash = ComputeMovementHash(testMovements);
        var cacheKey = $"validation:{boundaryHash}:{movementHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_validationCacheOptions);
            _logger.LogDebug("Computing boundary validation for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.ValidateBoundaries(boundaries, testMovements);
            LogCacheStatistics(cacheKey, "validation", false, 1);
            return result;
        }) ?? new ValidationResult();
    }

    public List<DynamicBoundaryWindow> FindDynamicBoundaries(List<PriceMovement> movements, int windowSize, int stepSize)
    {
        if (!_config.EnableCaching || movements == null)
            return _innerOptimizer.FindDynamicBoundaries(movements!, windowSize, stepSize) ?? new List<DynamicBoundaryWindow>();

        var cacheKey = $"dynamic:{ComputeMovementHash(movements)}:{windowSize}:{stepSize}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_optimizationCacheOptions);
            _logger.LogDebug("Computing dynamic boundaries for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.FindDynamicBoundaries(movements, windowSize, stepSize);
            LogCacheStatistics(cacheKey, "dynamic_boundaries", false, result?.Count ?? 0);
            return result ?? new List<DynamicBoundaryWindow>();
        }) ?? new List<DynamicBoundaryWindow>();
    }

    public List<ParetoSolution> OptimizeForMultipleObjectives(List<PriceMovement> movements, List<OptimizationObjective> objectives)
    {
        if (!_config.EnableCaching || movements == null || objectives == null)
            return _innerOptimizer.OptimizeForMultipleObjectives(movements!, objectives!) ?? new List<ParetoSolution>();

        var objectiveHash = string.Join(",", objectives.Select(ComputeObjectiveHash));
        var cacheKey = $"pareto:{ComputeMovementHash(movements)}:{objectiveHash}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_combinedCacheOptions);
            _logger.LogDebug("Computing Pareto optimization for cache key: {CacheKey}", cacheKey);
            
            var result = _innerOptimizer.OptimizeForMultipleObjectives(movements, objectives);
            LogCacheStatistics(cacheKey, "pareto_optimization", false, result?.Count ?? 0);
            return result ?? new List<ParetoSolution>();
        }) ?? new List<ParetoSolution>();
    }

    // Cross-Validation Methods (with caching)
    public CrossValidationResult KFoldCrossValidation(List<PriceMovement> data, int k = 5)
    {
        if (!_config.EnableCaching || data == null || data.Count == 0)
            return _innerOptimizer.KFoldCrossValidation(data!, k);

        var cacheKey = $"kfold:{ComputeMovementHash(data)}:{k}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_crossValidationCacheOptions);
            _logger.LogDebug("Computing K-Fold CV for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerOptimizer.KFoldCrossValidation(data, k);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogCacheStatistics(cacheKey, "kfold_cv", false, result?.FoldResults.Count ?? 0);
            return result ?? new CrossValidationResult();
        }) ?? new CrossValidationResult();
    }

    public CrossValidationResult TimeSeriesKFold(List<PriceMovement> data, int k = 5)
    {
        if (!_config.EnableCaching || data == null || data.Count == 0)
            return _innerOptimizer.TimeSeriesKFold(data!, k);

        var cacheKey = $"ts_kfold:{ComputeMovementHash(data)}:{k}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_crossValidationCacheOptions);
            _logger.LogDebug("Computing Time-Series K-Fold CV for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerOptimizer.TimeSeriesKFold(data, k);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogCacheStatistics(cacheKey, "ts_kfold_cv", false, result?.FoldResults.Count ?? 0);
            return result ?? new CrossValidationResult();
        }) ?? new CrossValidationResult();
    }

    public TimeSeriesCrossValidationResult ExpandingWindowValidation(List<PriceMovement> data, double initialSize, double stepSize)
    {
        if (!_config.EnableCaching || data == null || data.Count == 0)
            return _innerOptimizer.ExpandingWindowValidation(data!, initialSize, stepSize);

        var cacheKey = $"expanding:{ComputeMovementHash(data)}:{initialSize:F3}:{stepSize:F3}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_crossValidationCacheOptions);
            _logger.LogDebug("Computing Expanding Window CV for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerOptimizer.ExpandingWindowValidation(data, initialSize, stepSize);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogCacheStatistics(cacheKey, "expanding_cv", false, result?.FoldResults.Count ?? 0);
            return result ?? new TimeSeriesCrossValidationResult();
        }) ?? new TimeSeriesCrossValidationResult();
    }

    public TimeSeriesCrossValidationResult RollingWindowValidation(List<PriceMovement> data, double windowSize, double stepSize)
    {
        if (!_config.EnableCaching || data == null || data.Count == 0)
            return _innerOptimizer.RollingWindowValidation(data!, windowSize, stepSize);

        var cacheKey = $"rolling:{ComputeMovementHash(data)}:{windowSize:F3}:{stepSize:F3}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_crossValidationCacheOptions);
            _logger.LogDebug("Computing Rolling Window CV for cache key: {CacheKey}", cacheKey);
            
            var startTime = DateTime.UtcNow;
            var result = _innerOptimizer.RollingWindowValidation(data, windowSize, stepSize);
            var elapsed = DateTime.UtcNow - startTime;
            
            LogCacheStatistics(cacheKey, "rolling_cv", false, result?.FoldResults.Count ?? 0);
            return result ?? new TimeSeriesCrossValidationResult();
        }) ?? new TimeSeriesCrossValidationResult();
    }

    /// <summary>
    /// Gets cache performance metrics for optimization operations
    /// </summary>
    public OptimizationCacheStatistics GetCacheStatistics()
    {
        return new OptimizationCacheStatistics
        {
            BoundaryOptimizationCacheHits = 0, // Would need custom metrics tracking
            ClusteringCacheHits = 0,
            GradientSearchCacheHits = 0,
            CombinedOptimizationCacheHits = 0,
            ValidationCacheHits = 0,
            AverageComputationTimeSaved = TimeSpan.Zero,
            CacheHitRatio = 0.0
        };
    }

    /// <summary>
    /// Invalidates cached results for specific data patterns
    /// </summary>
    public void InvalidateCacheForData(string dataPattern)
    {
        _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", dataPattern);
        // Implementation would require custom cache with pattern-based invalidation
    }

    private string ComputeMovementHash(List<PriceMovement> movements)
    {
        if (movements == null || movements.Count == 0)
            return "empty";

        return $"{movements.Count}_{movements.First().StartTimestamp:yyyyMMddHH}_{movements.Last().StartTimestamp:yyyyMMddHH}";
    }

    private string ComputeObjectiveHash(OptimizationObjective objective)
    {
        if (objective == null)
            return "default";

        return $"{objective.Target}_{objective.Weight:F2}_{objective.MinATRMove:F4}";
    }

    private string ComputeMLConfigHash(MLOptimizationConfig config)
    {
        if (config == null)
            return "default";

        return $"{config.MaxIterations}_{config.ConvergenceThreshold:F6}_{config.UseDecisionTree}_{config.UseClustering}";
    }

    private string ComputeBoundaryHash(List<OptimalBoundary> boundaries)
    {
        if (boundaries == null || boundaries.Count == 0)
            return "empty";

        return $"{boundaries.Count}_{boundaries.Sum(b => b.RangeLow + b.RangeHigh):F4}";
    }

    private void LogCacheStatistics(string cacheKey, string operationType, bool wasHit, int resultCount)
    {
        var hitStatus = wasHit ? "HIT" : "MISS";
        _logger.LogDebug("Cache {HitStatus} for {OperationType}: {CacheKey} (result count: {ResultCount})", 
            hitStatus, operationType, cacheKey, resultCount);
    }
}

/// <summary>
/// Cache statistics specific to ML optimization operations
/// </summary>
public class OptimizationCacheStatistics
{
    public int BoundaryOptimizationCacheHits { get; set; }
    public int ClusteringCacheHits { get; set; }
    public int GradientSearchCacheHits { get; set; }
    public int CombinedOptimizationCacheHits { get; set; }
    public int ValidationCacheHits { get; set; }
    public TimeSpan AverageComputationTimeSaved { get; set; }
    public double CacheHitRatio { get; set; }
}