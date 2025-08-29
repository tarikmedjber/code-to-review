using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Statistics.Correlation.Models;

namespace MedjCap.Data.Infrastructure.Caching;

/// <summary>
/// Generates consistent cache keys for various calculation types based on input parameters
/// </summary>
public static class CacheKeyGenerator
{
    private const string CORRELATION_PREFIX = "corr";
    private const string STATISTICAL_PREFIX = "stat";
    private const string OPTIMIZATION_PREFIX = "opt";
    private const string ANALYSIS_PREFIX = "analysis";

    /// <summary>
    /// Generates a cache key for correlation calculations
    /// </summary>
    public static string ForCorrelation(List<PriceMovement> movements, CorrelationType correlationType, decimal? targetATR = null)
    {
        var dataHash = ComputeDataHash(movements);
        var key = $"{CORRELATION_PREFIX}:{correlationType}:{dataHash}";
        
        if (targetATR.HasValue)
        {
            key += $":{targetATR.Value:F4}";
        }
        
        return key;
    }

    /// <summary>
    /// Generates a cache key for statistical significance calculations
    /// </summary>
    public static string ForStatisticalSignificance(double coefficient, int sampleSize, double confidenceLevel)
    {
        return $"{STATISTICAL_PREFIX}:{coefficient:F6}:{sampleSize}:{confidenceLevel:F3}";
    }

    /// <summary>
    /// Generates a cache key for ML boundary optimization results
    /// </summary>
    public static string ForOptimization(List<PriceMovement> movements, OptimizationTarget target, AnalysisConfig config)
    {
        var dataHash = ComputeDataHash(movements);
        var configHash = ComputeConfigHash(config);
        return $"{OPTIMIZATION_PREFIX}:{target}:{dataHash}:{configHash}";
    }

    /// <summary>
    /// Generates a cache key for complete analysis results
    /// </summary>
    public static string ForAnalysis(List<PriceMovement> movements, AnalysisConfig config, string analysisType = "complete")
    {
        var dataHash = ComputeDataHash(movements);
        var configHash = ComputeConfigHash(config);
        return $"{ANALYSIS_PREFIX}:{analysisType}:{dataHash}:{configHash}";
    }

    /// <summary>
    /// Generates a cache key for walk-forward analysis results
    /// </summary>
    public static string ForWalkForward(List<PriceMovement> movements, AnalysisConfig config, OptimizationTarget target)
    {
        var dataHash = ComputeDataHash(movements);
        var configHash = ComputeConfigHash(config);
        return $"{ANALYSIS_PREFIX}:walkforward:{target}:{dataHash}:{configHash}";
    }

    /// <summary>
    /// Computes a hash of price movement data for cache key consistency
    /// </summary>
    private static string ComputeDataHash(List<PriceMovement> movements)
    {
        if (movements == null || movements.Count == 0)
            return "empty";

        // Create a consistent hash based on key properties
        var hashInput = new StringBuilder();
        hashInput.Append($"count:{movements.Count}");
        hashInput.Append($"first:{movements.First().StartTimestamp:yyyyMMddHHmm}");
        hashInput.Append($"last:{movements.Last().StartTimestamp:yyyyMMddHHmm}");
        
        // Sample a few data points for hash consistency without full data dependency
        var sampleIndices = new[] { 0, movements.Count / 4, movements.Count / 2, 3 * movements.Count / 4, movements.Count - 1 };
        foreach (var index in sampleIndices.Where(i => i < movements.Count))
        {
            var movement = movements[index];
            hashInput.Append($"{movement.ATRMovement:F4}:{movement.MeasurementValue:F4}");
        }

        return ComputeHash(hashInput.ToString())[..12]; // First 12 characters for brevity
    }

    /// <summary>
    /// Computes a hash of analysis configuration for cache key consistency
    /// </summary>
    private static string ComputeConfigHash(AnalysisConfig config)
    {
        if (config == null)
            return "default";

        var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        return ComputeHash(configJson)[..8]; // First 8 characters for brevity
    }

    /// <summary>
    /// Computes SHA256 hash of input string
    /// </summary>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('='); // URL-safe base64
    }

    /// <summary>
    /// Extracts cache key components for debugging and monitoring
    /// </summary>
    public static (string Prefix, string[] Components) ParseCacheKey(string cacheKey)
    {
        var parts = cacheKey.Split(':');
        if (parts.Length == 0)
            return (string.Empty, Array.Empty<string>());

        return (parts[0], parts.Skip(1).ToArray());
    }
}