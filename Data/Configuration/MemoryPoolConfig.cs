using System;

namespace MedjCap.Data.Configuration;

/// <summary>
/// Configuration for array memory pooling optimization settings.
/// Controls memory allocation patterns and pool sizing for data analysis operations.
/// </summary>
public class MemoryPoolConfig
{
    /// <summary>
    /// Maximum length for double arrays that will be pooled.
    /// Arrays larger than this will be allocated normally.
    /// Default: 100,000 elements (~800KB)
    /// </summary>
    public int MaxDoubleArrayLength { get; set; } = 100_000;

    /// <summary>
    /// Maximum length for decimal arrays that will be pooled.
    /// Arrays larger than this will be allocated normally.
    /// Default: 100,000 elements (~1.6MB)
    /// </summary>
    public int MaxDecimalArrayLength { get; set; } = 100_000;

    /// <summary>
    /// Maximum length for int arrays that will be pooled.
    /// Arrays larger than this will be allocated normally.
    /// Default: 100,000 elements (~400KB)
    /// </summary>
    public int MaxIntArrayLength { get; set; } = 100_000;

    /// <summary>
    /// Maximum number of arrays to keep in each pool bucket.
    /// Higher values reduce allocations but use more memory.
    /// Default: 50 arrays per size bucket
    /// </summary>
    public int MaxArraysPerBucket { get; set; } = 50;

    /// <summary>
    /// Whether to enable array pooling at all.
    /// Can be disabled for debugging or in memory-constrained environments.
    /// Default: true
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// Whether to clear arrays when returning them to the pool.
    /// Improves security but has slight performance cost.
    /// Default: true
    /// </summary>
    public bool ClearArraysOnReturn { get; set; } = true;

    /// <summary>
    /// Whether to track detailed statistics about pool usage.
    /// Useful for monitoring but has slight performance overhead.
    /// Default: true in development, false in production
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Minimum array size that should use pooling.
    /// Smaller arrays are cheaper to allocate than pool management overhead.
    /// Default: 256 elements
    /// </summary>
    public int MinPooledArraySize { get; set; } = 256;

    /// <summary>
    /// Target memory usage limit for all pools combined (in bytes).
    /// When exceeded, pools will be trimmed. 0 = unlimited.
    /// Default: 100MB
    /// </summary>
    public long MaxTotalPoolMemory { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// How often to check and trim pools when memory limit is exceeded.
    /// Default: every 1000 operations
    /// </summary>
    public int PoolTrimInterval { get; set; } = 1000;
}

/// <summary>
/// Extension methods for memory pool configuration.
/// </summary>
public static class MemoryPoolConfigExtensions
{
    /// <summary>
    /// Validates the memory pool configuration and provides sensible defaults.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>True if configuration is valid.</returns>
    public static bool Validate(this MemoryPoolConfig config)
    {
        if (config.MaxDoubleArrayLength <= 0 || config.MaxDoubleArrayLength > 10_000_000)
            return false;
            
        if (config.MaxDecimalArrayLength <= 0 || config.MaxDecimalArrayLength > 10_000_000)
            return false;
            
        if (config.MaxIntArrayLength <= 0 || config.MaxIntArrayLength > 10_000_000)
            return false;
            
        if (config.MaxArraysPerBucket <= 0 || config.MaxArraysPerBucket > 1000)
            return false;
            
        if (config.MinPooledArraySize < 0 || config.MinPooledArraySize > config.MaxDoubleArrayLength)
            return false;
            
        if (config.MaxTotalPoolMemory < 0)
            return false;
            
        if (config.PoolTrimInterval <= 0)
            return false;
            
        return true;
    }

    /// <summary>
    /// Calculates the estimated maximum memory usage for the pool configuration.
    /// </summary>
    /// <param name="config">The configuration to analyze.</param>
    /// <returns>Estimated maximum memory usage in bytes.</returns>
    public static long EstimateMaxMemoryUsage(this MemoryPoolConfig config)
    {
        if (!config.EnablePooling)
            return 0;

        // Rough estimate: assume average array is half the maximum size
        var avgDoubleArrayBytes = (config.MaxDoubleArrayLength / 2) * sizeof(double) * config.MaxArraysPerBucket;
        var avgDecimalArrayBytes = (config.MaxDecimalArrayLength / 2) * sizeof(decimal) * config.MaxArraysPerBucket;
        var avgIntArrayBytes = (config.MaxIntArrayLength / 2) * sizeof(int) * config.MaxArraysPerBucket;

        // ArrayPool uses multiple buckets for different sizes, estimate ~10 buckets per type
        return (avgDoubleArrayBytes + avgDecimalArrayBytes + avgIntArrayBytes) * 10;
    }

    /// <summary>
    /// Applies performance-optimized settings for high-frequency trading scenarios.
    /// </summary>
    /// <param name="config">The configuration to optimize.</param>
    /// <returns>The configuration with optimized settings.</returns>
    public static MemoryPoolConfig OptimizeForHighFrequency(this MemoryPoolConfig config)
    {
        config.MaxArraysPerBucket = 100; // More arrays cached
        config.ClearArraysOnReturn = false; // Skip clearing for performance
        config.EnableStatistics = false; // Disable statistics overhead
        config.MinPooledArraySize = 64; // Pool even smaller arrays
        config.PoolTrimInterval = 5000; // Less frequent trimming
        return config;
    }

    /// <summary>
    /// Applies memory-conservative settings for resource-constrained environments.
    /// </summary>
    /// <param name="config">The configuration to optimize.</param>
    /// <returns>The configuration with conservative settings.</returns>
    public static MemoryPoolConfig OptimizeForMemory(this MemoryPoolConfig config)
    {
        config.MaxArraysPerBucket = 10; // Fewer cached arrays
        config.MaxDoubleArrayLength = 10_000; // Smaller max arrays
        config.MaxDecimalArrayLength = 10_000;
        config.MaxIntArrayLength = 10_000;
        config.MinPooledArraySize = 1024; // Only pool larger arrays
        config.MaxTotalPoolMemory = 10 * 1024 * 1024; // 10MB limit
        config.PoolTrimInterval = 100; // Frequent trimming
        return config;
    }
}