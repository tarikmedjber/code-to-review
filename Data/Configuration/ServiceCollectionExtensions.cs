using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MedjCap.Data.Core;
using MedjCap.Data.Services;
using MedjCap.Data.Services.OptimizationStrategies;
using MedjCap.Data.Storage;
using MedjCap.Data.Repository;
using MedjCap.Data.Configuration;

namespace MedjCap.Data.Configuration;

/// <summary>
/// Dependency injection container configuration for MedjCap.Data services.
/// Provides extension methods to register all analysis services with proper lifetimes.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all MedjCap.Data analysis services with default configurations.
    /// Uses in-memory storage and default statistical configurations suitable for development.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMedjCapDataServices(this IServiceCollection services)
    {
        return services.AddMedjCapDataServices(options => { });
    }

    /// <summary>
    /// Registers all MedjCap.Data analysis services with custom configuration.
    /// Allows fine-tuning of statistical parameters, caching, and optimization settings.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="configureOptions">Action to configure analysis options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMedjCapDataServices(this IServiceCollection services, 
        Action<MedjCapDataOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);
        
        // Register storage layer (in-memory by default)
        services.AddMedjCapDataStorage();
        
        // Register core services
        services.AddMedjCapCoreServices();
        
        // Register analysis services
        services.AddMedjCapAnalysisServices();
        
        // Register optimization services
        services.AddMedjCapOptimizationServices();
        
        return services;
    }

    /// <summary>
    /// Registers MedjCap.Data services with caching enabled for production environments.
    /// Provides enhanced performance through intelligent caching of expensive calculations.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="configureOptions">Action to configure analysis and caching options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMedjCapDataServicesWithCaching(this IServiceCollection services, 
        Action<MedjCapDataOptions> configureOptions)
    {
        // Configure options with caching enabled
        services.Configure<MedjCapDataOptions>(options =>
        {
            options.EnableCaching = true;
            configureOptions(options);
        });
        
        // Register memory caching
        services.AddMemoryCache();
        
        // Register storage layer
        services.AddMedjCapDataStorage();
        
        // Register core services
        services.AddMedjCapCoreServices();
        
        // Register cached analysis services (decorators)
        services.AddMedjCapCachedAnalysisServices();
        
        // Register optimization services
        services.AddMedjCapOptimizationServices();
        
        return services;
    }

    private static IServiceCollection AddMedjCapDataStorage(this IServiceCollection services)
    {
        // Register storage implementations
        services.AddSingleton<ITimeSeriesDataStorage, InMemoryTimeSeriesDataStorage>();
        services.AddSingleton<IAnalysisRepository, AnalysisRepository>();
        
        return services;
    }

    private static IServiceCollection AddMedjCapCoreServices(this IServiceCollection services)
    {
        // Register data collection services
        services.AddScoped<IDataCollector, DataCollector>();
        
        // Register validation services
        services.AddScoped<IAnalysisValidator, AnalysisValidator>();
        
        // Note: IAnalysisEngine implementation can be added when fully implemented
        
        // Register orchestration services
        services.AddScoped<IAnalysisOrchestrator, AnalysisOrchestrator>();
        services.AddScoped<IAnalysisWorkflowFactory, AnalysisWorkflowFactory>();
        services.AddScoped<IAnalysisCalculationEngine, AnalysisCalculationEngine>();
        services.AddScoped<IAnalysisResultFormatter, AnalysisResultFormatter>();
        services.AddScoped<IAnalysisResultAggregator, AnalysisResultAggregator>();
        
        return services;
    }

    private static IServiceCollection AddMedjCapAnalysisServices(this IServiceCollection services)
    {
        // Register core analysis services
        services.AddScoped<ICorrelationService, CorrelationService>();
        services.AddScoped<IOutlierDetectionService, OutlierDetectionService>();
        services.AddScoped<IBacktestService, BacktestService>();
        
        // Register cross-validation services
        services.AddScoped<CrossValidationService>();
        
        // Note: Validation strategies can be registered separately if needed
        
        return services;
    }

    private static IServiceCollection AddMedjCapCachedAnalysisServices(this IServiceCollection services)
    {
        // Register cached analysis services as decorators
        services.AddScoped<CorrelationService>(); // Base implementation
        services.AddScoped<ICorrelationService, CachedCorrelationService>();
        
        services.AddScoped<MLBoundaryOptimizer>(); // Base implementation
        services.AddScoped<IMLBoundaryOptimizer, CachedMLBoundaryOptimizer>();
        
        services.AddScoped<BacktestService>(); // Base implementation
        services.AddScoped<IBacktestService, CachedBacktestService>();
        
        services.AddScoped<IOutlierDetectionService, OutlierDetectionService>();
        
        // Register cross-validation services
        services.AddScoped<CrossValidationService>();
        
        // Note: Validation strategies can be registered separately if needed
        
        return services;
    }

    private static IServiceCollection AddMedjCapOptimizationServices(this IServiceCollection services)
    {
        // Register optimization strategy factory and strategies
        services.AddScoped<IOptimizationStrategyFactory, OptimizationStrategyFactory>();
        services.AddScoped<IOptimizationStrategy, DecisionTreeOptimizationStrategy>();
        services.AddScoped<IOptimizationStrategy, ClusteringOptimizationStrategy>();
        services.AddScoped<IOptimizationStrategy, GradientSearchOptimizationStrategy>();
        
        // Register ML boundary optimizer (cached version registered in cached services)
        services.AddScoped<IMLBoundaryOptimizer, MLBoundaryOptimizer>();
        
        return services;
    }
}

/// <summary>
/// Configuration options for MedjCap.Data services.
/// Controls analysis parameters, caching behavior, and service lifetimes.
/// </summary>
public class MedjCapDataOptions
{
    /// <summary>
    /// Statistical analysis configuration parameters.
    /// </summary>
    public StatisticalConfig StatisticalConfig { get; set; } = new();

    /// <summary>
    /// ML boundary optimization configuration parameters.
    /// </summary>
    public OptimizationConfig OptimizationConfig { get; set; } = new();

    /// <summary>
    /// Validation and cross-validation configuration parameters.
    /// </summary>
    public ValidationConfig ValidationConfig { get; set; } = new();

    /// <summary>
    /// Caching configuration for expensive operations.
    /// </summary>
    public CachingConfig CachingConfig { get; set; } = new();

    /// <summary>
    /// Enable caching for expensive analysis operations (default: false for development).
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Enable comprehensive logging for debugging and performance monitoring (default: false).
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Validation strictness level for analysis requests (default: Standard).
    /// </summary>
    public ValidationStrictness ValidationStrictness { get; set; } = ValidationStrictness.Standard;
}

/// <summary>
/// Defines the level of validation strictness for analysis requests.
/// </summary>
public enum ValidationStrictness
{
    /// <summary>
    /// Minimal validation - allows most analysis requests to proceed.
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard validation - reasonable checks for data quality and parameter validity.
    /// </summary>
    Standard,

    /// <summary>
    /// Strict validation - comprehensive validation with strict parameter checking.
    /// </summary>
    Strict,

    /// <summary>
    /// Production validation - all validations plus performance and resource checks.
    /// </summary>
    Production
}