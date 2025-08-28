using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MedjCap.Data.Core;
using MedjCap.Data.Services;

namespace MedjCap.Data.Configuration;

/// <summary>
/// Factory for creating MedjCap analysis services with proper dependency injection.
/// Provides simplified service creation for scenarios where full DI container isn't available.
/// </summary>
public class MedjCapAnalysisServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public MedjCapAnalysisServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    // Note: Analysis engine creation will be available when IAnalysisEngine implementation is complete

    /// <summary>
    /// Creates a correlation service with resolved dependencies.
    /// </summary>
    /// <returns>Configured correlation service</returns>
    public ICorrelationService CreateCorrelationService()
    {
        return _serviceProvider.GetRequiredService<ICorrelationService>();
    }

    /// <summary>
    /// Creates an ML boundary optimizer with resolved dependencies and strategies.
    /// </summary>
    /// <returns>Configured ML boundary optimizer</returns>
    public IMLBoundaryOptimizer CreateMLBoundaryOptimizer()
    {
        return _serviceProvider.GetRequiredService<IMLBoundaryOptimizer>();
    }

    /// <summary>
    /// Creates a data collector for gathering time series data.
    /// </summary>
    /// <returns>Configured data collector</returns>
    public IDataCollector CreateDataCollector()
    {
        return _serviceProvider.GetRequiredService<IDataCollector>();
    }

    /// <summary>
    /// Creates an outlier detection service with configured algorithms.
    /// </summary>
    /// <returns>Configured outlier detection service</returns>
    public IOutlierDetectionService CreateOutlierDetectionService()
    {
        return _serviceProvider.GetRequiredService<IOutlierDetectionService>();
    }

    /// <summary>
    /// Creates a backtest service for strategy validation.
    /// </summary>
    /// <returns>Configured backtest service</returns>
    public IBacktestService CreateBacktestService()
    {
        return _serviceProvider.GetRequiredService<IBacktestService>();
    }
    
    /// <summary>
    /// Creates a complete analysis workflow orchestrator.
    /// </summary>
    /// <returns>Configured analysis orchestrator</returns>
    public IAnalysisOrchestrator CreateAnalysisOrchestrator()
    {
        return _serviceProvider.GetRequiredService<IAnalysisOrchestrator>();
    }
}

/// <summary>
/// Static factory for creating MedjCap analysis services without explicit DI container setup.
/// Useful for simple scenarios, testing, or when integrating with existing applications.
/// </summary>
public static class MedjCapServicesFactory
{
    /// <summary>
    /// Creates a service provider with default MedjCap.Data services configured.
    /// Uses in-memory storage and default configurations suitable for development and testing.
    /// </summary>
    /// <returns>Service provider with all MedjCap.Data services registered</returns>
    public static IServiceProvider CreateServiceProvider()
    {
        return CreateServiceProvider(options => { });
    }

    /// <summary>
    /// Creates a service provider with custom MedjCap.Data service configuration.
    /// </summary>
    /// <param name="configureOptions">Action to configure service options</param>
    /// <returns>Service provider with configured MedjCap.Data services</returns>
    public static IServiceProvider CreateServiceProvider(Action<MedjCapDataOptions> configureOptions)
    {
        var services = new ServiceCollection();
        services.AddMedjCapDataServices(configureOptions);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with caching enabled for production scenarios.
    /// </summary>
    /// <param name="configureOptions">Action to configure service and caching options</param>
    /// <returns>Service provider with cached MedjCap.Data services</returns>
    public static IServiceProvider CreateCachedServiceProvider(Action<MedjCapDataOptions> configureOptions)
    {
        var services = new ServiceCollection();
        services.AddMedjCapDataServicesWithCaching(configureOptions);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates an analysis service factory with default configuration.
    /// Convenient for scenarios where you need multiple service instances.
    /// </summary>
    /// <returns>Analysis service factory ready for use</returns>
    public static MedjCapAnalysisServiceFactory CreateAnalysisServiceFactory()
    {
        var serviceProvider = CreateServiceProvider();
        return new MedjCapAnalysisServiceFactory(serviceProvider);
    }

    /// <summary>
    /// Creates an analysis service factory with custom configuration.
    /// </summary>
    /// <param name="configureOptions">Action to configure service options</param>
    /// <returns>Analysis service factory with custom configuration</returns>
    public static MedjCapAnalysisServiceFactory CreateAnalysisServiceFactory(Action<MedjCapDataOptions> configureOptions)
    {
        var serviceProvider = CreateServiceProvider(configureOptions);
        return new MedjCapAnalysisServiceFactory(serviceProvider);
    }

    /// <summary>
    /// Creates a cached analysis service factory for production scenarios.
    /// </summary>
    /// <param name="configureOptions">Action to configure service and caching options</param>
    /// <returns>Analysis service factory with caching enabled</returns>
    public static MedjCapAnalysisServiceFactory CreateCachedAnalysisServiceFactory(Action<MedjCapDataOptions> configureOptions)
    {
        var serviceProvider = CreateCachedServiceProvider(configureOptions);
        return new MedjCapAnalysisServiceFactory(serviceProvider);
    }

    /// <summary>
    /// Quick method to create a correlation service with default settings.
    /// Useful for simple correlation analysis scenarios.
    /// </summary>
    /// <returns>Ready-to-use correlation service</returns>
    public static ICorrelationService CreateCorrelationService()
    {
        var serviceProvider = CreateServiceProvider();
        return serviceProvider.GetRequiredService<ICorrelationService>();
    }

    /// <summary>
    /// Quick method to create an ML boundary optimizer with default settings.
    /// Useful for boundary optimization tasks.
    /// </summary>
    /// <returns>Ready-to-use ML boundary optimizer</returns>
    public static IMLBoundaryOptimizer CreateMLBoundaryOptimizer()
    {
        var serviceProvider = CreateServiceProvider();
        return serviceProvider.GetRequiredService<IMLBoundaryOptimizer>();
    }

    /// <summary>
    /// Quick method to create a data collector with default settings.
    /// Useful for data collection scenarios.
    /// </summary>
    /// <returns>Ready-to-use data collector</returns>
    public static IDataCollector CreateDataCollector()
    {
        var serviceProvider = CreateServiceProvider();
        return serviceProvider.GetRequiredService<IDataCollector>();
    }
}