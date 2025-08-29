using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Statistics.Services;
using MedjCap.Data.Analysis.Services;
using MedjCap.Data.MachineLearning.Services;
using MedjCap.Data.DataQuality.Services;
using MedjCap.Data.Backtesting.Services;
using MedjCap.Data.TimeSeries.Services;
using MedjCap.Data.Infrastructure.Caching;
using MedjCap.Data.Infrastructure.MemoryManagement;
using System;

namespace MedjCap.Data.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering memory pool services in the dependency injection container.
/// Provides fluent configuration for memory optimization features.
/// </summary>
public static class MemoryPoolServiceExtensions
{
    /// <summary>
    /// Adds array memory pooling services to the service collection with default configuration.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMemoryPooling(this IServiceCollection services)
    {
        return services.AddMemoryPooling(options => { });
    }

    /// <summary>
    /// Adds array memory pooling services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configureOptions">Action to configure memory pool options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMemoryPooling(
        this IServiceCollection services,
        Action<MemoryPoolConfig> configureOptions)
    {
        // Configure memory pool options
        services.Configure(configureOptions);

        // Validate configuration
        services.AddSingleton<IValidateOptions<MemoryPoolConfig>>(serviceProvider =>
            new ValidateOptions<MemoryPoolConfig>(
                "MemoryPool",
                config => config.Validate(),
                "Invalid memory pool configuration"));

        // Register array memory pool as singleton for optimal performance
        services.AddSingleton<IArrayMemoryPool>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MemoryPoolConfig>>();
            var config = options.Value;

            if (!config.EnablePooling)
                return new NullArrayMemoryPool(); // No-op implementation when disabled

            return new ArrayMemoryPool(
                config.MaxDoubleArrayLength,
                config.MaxDecimalArrayLength,
                config.MaxIntArrayLength);
        });

        return services;
    }

    /// <summary>
    /// Adds memory-optimized versions of data analysis services that use array pooling.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMemoryOptimizedAnalysis(this IServiceCollection services)
    {
        // Ensure memory pooling is available
        services.TryAddSingleton<IArrayMemoryPool, ArrayMemoryPool>();

        // Register memory-optimized versions of services
        services.AddScoped<MemoryOptimizedCorrelationService>();
        
        // Option to replace default implementations with memory-optimized versions
        // Uncomment to use memory-optimized versions by default:
        // services.AddScoped<ICorrelationService, MemoryOptimizedCorrelationService>();

        return services;
    }

    /// <summary>
    /// Configures memory pooling for high-frequency analysis scenarios.
    /// Optimizes for performance over memory usage.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHighFrequencyMemoryPooling(this IServiceCollection services)
    {
        return services.AddMemoryPooling(options =>
        {
            options.OptimizeForHighFrequency();
        });
    }

    /// <summary>
    /// Configures memory pooling for memory-constrained environments.
    /// Optimizes for minimal memory footprint.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConservativeMemoryPooling(this IServiceCollection services)
    {
        return services.AddMemoryPooling(options =>
        {
            options.OptimizeForMemory();
        });
    }

    /// <summary>
    /// Helper method to add try-add pattern for singleton services.
    /// </summary>
    private static void TryAddSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(x => x.ServiceType == typeof(TService)))
        {
            services.AddSingleton<TService, TImplementation>();
        }
    }

    /// <summary>
    /// Helper method to check if a service is already registered.
    /// </summary>
    private static bool Any(this IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        foreach (var service in services)
        {
            if (predicate(service))
                return true;
        }
        return false;
    }
}

/// <summary>
/// No-operation implementation of IArrayMemoryPool for when pooling is disabled.
/// Always allocates new arrays and ignores return calls.
/// </summary>
internal class NullArrayMemoryPool : IArrayMemoryPool
{
    public double[] RentDoubleArray(int minimumLength) => new double[minimumLength];
    public decimal[] RentDecimalArray(int minimumLength) => new decimal[minimumLength];
    public int[] RentIntArray(int minimumLength) => new int[minimumLength];
    public void ReturnDoubleArray(double[]? array, bool clearArray = true) { }
    public void ReturnDecimalArray(decimal[]? array, bool clearArray = true) { }
    public void ReturnIntArray(int[]? array, bool clearArray = true) { }

    public ArrayPoolStatistics GetStatistics() => new ArrayPoolStatistics
    {
        DoubleArraysRented = 0,
        DecimalArraysRented = 0,
        IntArraysRented = 0,
        DoubleArraysInPool = 0,
        DecimalArraysInPool = 0,
        IntArraysInPool = 0,
        TotalBytesAllocated = 0,
        TotalBytesInPool = 0,
        PoolHitRatio = 0.0
    };
}

/// <summary>
/// Validation helper for memory pool configuration.
/// </summary>
internal class ValidateOptions<T> : IValidateOptions<T> where T : class
{
    private readonly string _name;
    private readonly Func<T, bool> _validation;
    private readonly string _failureMessage;

    public ValidateOptions(string name, Func<T, bool> validation, string failureMessage)
    {
        _name = name;
        _validation = validation;
        _failureMessage = failureMessage;
    }

    public ValidateOptionsResult Validate(string? name, T options)
    {
        if (name != null && !string.Equals(name, _name, StringComparison.Ordinal))
            return ValidateOptionsResult.Skip;

        if (_validation(options))
            return ValidateOptionsResult.Success;

        return ValidateOptionsResult.Fail(_failureMessage);
    }
}