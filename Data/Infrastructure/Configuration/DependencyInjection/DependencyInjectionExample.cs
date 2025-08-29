using Microsoft.Extensions.DependencyInjection;
using MedjCap.Data.Infrastructure.Configuration.DependencyInjection;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.Statistics.Models;
using MedjCap.Data.Infrastructure.Models;
using MedjCap.Data.Statistics.Correlation.Models;
using MedjCap.Data.DataQuality.Models;

namespace MedjCap.Data.Infrastructure.Configuration.DependencyInjection;

/// <summary>
/// Example demonstrating how to use the MedjCap.Data dependency injection container.
/// Shows various ways to configure and consume analysis services.
/// </summary>
public static class DependencyInjectionExample
{
    /// <summary>
    /// Demonstrates basic service creation and usage with default configuration.
    /// </summary>
    public static void BasicUsageExample()
    {
        // Create service provider with default settings
        var serviceProvider = MedjCapServicesFactory.CreateServiceProvider();

        // Get services from the container
        var correlationService = serviceProvider.GetRequiredService<ICorrelationService>();
        var mlOptimizer = serviceProvider.GetRequiredService<IMLBoundaryOptimizer>();
        var dataCollector = serviceProvider.GetRequiredService<IDataCollector>();
        var outlierService = serviceProvider.GetRequiredService<IOutlierDetectionService>();

        // Example usage - collect some test data
        var timestamp = DateTime.UtcNow;
        dataCollector.AddDataPoint(timestamp, "TestIndicator", 75.5m, 100.0m, 2.0m);
        dataCollector.AddDataPoint(timestamp.AddMinutes(1), "TestIndicator", 76.2m, 100.5m, 2.1m);
        dataCollector.AddDataPoint(timestamp.AddMinutes(2), "TestIndicator", 74.8m, 99.8m, 1.9m);

        // Get statistics
        var stats = dataCollector.GetStatistics();
        Console.WriteLine($"Collected {stats.TotalDataPoints} data points");
        
        // Dispose the service provider when done
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates custom configuration with specific analysis parameters.
    /// </summary>
    public static void CustomConfigurationExample()
    {
        var serviceProvider = MedjCapServicesFactory.CreateServiceProvider(options =>
        {
            // Configure statistical analysis parameters
            options.StatisticalConfig.StrengthThresholds.Strong = 0.8;
            options.StatisticalConfig.StrengthThresholds.Moderate = 0.5;
            options.StatisticalConfig.StrengthThresholds.Weak = 0.3;

            // Enable and configure outlier detection
            options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
            options.StatisticalConfig.OutlierDetection.ZScoreThreshold = 2.5;
            options.StatisticalConfig.OutlierDetection.DefaultHandlingStrategy = OutlierHandlingStrategy.Cap;

            // Configure ML optimization
            options.OptimizationConfig.MaxRanges = 8;
            options.OptimizationConfig.FeatureImportance.PrimaryFeatureImportance = 0.4;

            // Set validation parameters
            options.ValidationConfig.DefaultKFolds = 5;
            options.ValidationConfig.TrainTestSplit = 0.8;
            
            // Enable detailed logging
            options.EnableDetailedLogging = true;
        });

        var correlationService = serviceProvider.GetRequiredService<ICorrelationService>();
        var outlierService = serviceProvider.GetRequiredService<IOutlierDetectionService>();

        // Services are now configured with custom parameters
        Console.WriteLine("Services created with custom configuration");
        
        // Dispose when done
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates production setup with caching enabled for performance.
    /// </summary>
    public static void ProductionCachingExample()
    {
        var serviceProvider = MedjCapServicesFactory.CreateCachedServiceProvider(options =>
        {
            // Caching is automatically enabled
            options.CachingConfig.CorrelationCache.TTL = TimeSpan.FromMinutes(30);
            options.CachingConfig.OptimizationCache.TTL = TimeSpan.FromMinutes(60);
            options.CachingConfig.StatisticalCache.TTL = TimeSpan.FromMinutes(15);

            // Production validation settings
            options.ValidationStrictness = ValidationStrictness.Production;
            options.EnableDetailedLogging = false; // Disable in production for performance
            
            // Configure for production workloads
            options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
            options.ValidationConfig.TrainTestSplit = 0.8;
        });

        // These services will use intelligent caching
        var cachedCorrelationService = serviceProvider.GetRequiredService<ICorrelationService>();
        var cachedMLOptimizer = serviceProvider.GetRequiredService<IMLBoundaryOptimizer>();

        Console.WriteLine("Production services with caching enabled");
        
        // Dispose when done
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates using the service factory for simplified access.
    /// </summary>
    public static void ServiceFactoryExample()
    {
        // Create a service factory
        var factory = MedjCapServicesFactory.CreateAnalysisServiceFactory(options =>
        {
            options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
        });

        // Create services as needed
        var correlationService = factory.CreateCorrelationService();
        var mlOptimizer = factory.CreateMLBoundaryOptimizer();
        var dataCollector = factory.CreateDataCollector();
        var outlierService = factory.CreateOutlierDetectionService();

        Console.WriteLine("Services created via factory pattern");
    }

    /// <summary>
    /// Demonstrates quick service creation for simple scenarios.
    /// </summary>
    public static void QuickServiceExample()
    {
        // Direct service creation with defaults - great for simple scenarios
        var correlationService = MedjCapServicesFactory.CreateCorrelationService();
        var mlOptimizer = MedjCapServicesFactory.CreateMLBoundaryOptimizer();
        var dataCollector = MedjCapServicesFactory.CreateDataCollector();

        // Use services immediately
        Console.WriteLine("Quick service creation completed");
    }

    /// <summary>
    /// Demonstrates integration with an existing ASP.NET Core-like DI container.
    /// </summary>
    public static void ExistingContainerIntegrationExample()
    {
        // Simulate existing DI container
        var services = new ServiceCollection();
        
        // Add your existing services
        services.AddLogging();
        
        // Add MedjCap.Data services to existing container
        services.AddMedjCapDataServicesWithCaching(options =>
        {
            options.EnableCaching = true;
            options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
        });

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Now all services are available together
        var correlationService = serviceProvider.GetRequiredService<ICorrelationService>();
        var loggerFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DependencyInjectionExample");

        Console.WriteLine("Integration with existing DI container completed");
        
        // Dispose when done
        serviceProvider.Dispose();
    }

    /// <summary>
    /// Demonstrates advanced caching with invalidation and metrics collection.
    /// </summary>
    public static void AdvancedCachingWithMetricsExample()
    {
        var serviceProvider = MedjCapServicesFactory.CreateServiceProvider(options =>
        {
            // Enable comprehensive caching
            options.CachingConfig.EnableCaching = true;
            options.CachingConfig.CorrelationCache.TTL = TimeSpan.FromMinutes(30);
            options.CachingConfig.OptimizationCache.TTL = TimeSpan.FromHours(1);
            
            // Configure for production monitoring
            options.EnableDetailedLogging = true;
            options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
        });

        // Get services with caching, invalidation, and metrics
        var dataCollector = serviceProvider.GetRequiredService<IDataCollector>(); // Cache-aware version
        var correlationService = serviceProvider.GetRequiredService<ICorrelationService>(); // Cached version
        var invalidationService = serviceProvider.GetRequiredService<ICacheInvalidationService>();
        var metricsService = serviceProvider.GetRequiredService<ICacheMetricsService>();

        Console.WriteLine("=== Advanced Caching Demo ===");

        // Add some sample data
        var baseTime = DateTime.UtcNow.AddDays(-1);
        for (int i = 0; i < 100; i++)
        {
            dataCollector.AddDataPoint(
                baseTime.AddMinutes(i), 
                "TestIndicator", 
                50m + (decimal)Math.Sin(i * 0.1) * 10m, 
                1000m + i * 0.1m, 
                5m + (decimal)Math.Cos(i * 0.05) * 2m);
        }

        // Perform some cached operations
        var timeSeries = dataCollector.GetTimeSeriesData();
        var priceMovements = correlationService.CalculatePriceMovements(timeSeries, TimeSpan.FromMinutes(15));
        var correlation1 = correlationService.CalculateCorrelation(priceMovements, CorrelationType.Pearson);
        
        // These should be cache hits
        var correlation2 = correlationService.CalculateCorrelation(priceMovements, CorrelationType.Pearson);
        var correlation3 = correlationService.CalculateCorrelation(priceMovements, CorrelationType.Pearson);

        // Get cache performance metrics
        var metrics = metricsService.GetPerformanceMetrics();
        Console.WriteLine($"Cache Performance Report:");
        Console.WriteLine($"  Total Operations: {metrics.TotalOperations}");
        Console.WriteLine($"  Hit Rate: {metrics.OverallHitRate:P2}");
        Console.WriteLine($"  Average Duration: {metrics.AverageOperationDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Performance Score: {metrics.PerformanceScore}/100");

        // Demonstrate cache invalidation
        Console.WriteLine("\n=== Testing Cache Invalidation ===");
        
        // Add new data point - should trigger invalidation
        dataCollector.AddDataPoint(baseTime.AddMinutes(101), "TestIndicator", 60m, 1010m, 7m);
        
        // This should be a cache miss due to invalidation
        var newTimeSeries = dataCollector.GetTimeSeriesData();
        var newPriceMovements = correlationService.CalculatePriceMovements(newTimeSeries, TimeSpan.FromMinutes(15));
        
        // Get updated metrics
        var updatedMetrics = metricsService.GetPerformanceMetrics();
        Console.WriteLine($"After invalidation:");
        Console.WriteLine($"  Total Operations: {updatedMetrics.TotalOperations}");
        Console.WriteLine($"  Hit Rate: {updatedMetrics.OverallHitRate:P2}");
        Console.WriteLine($"  Total Invalidations: {updatedMetrics.TotalInvalidations}");

        // Show cache statistics by type
        Console.WriteLine("\n=== Cache Statistics by Type ===");
        foreach (var (cacheType, hitRate) in updatedMetrics.HitRatesByType)
        {
            var typeMetrics = metricsService.GetMetricsForCacheType(cacheType);
            if (typeMetrics != null)
            {
                Console.WriteLine($"  {cacheType}:");
                Console.WriteLine($"    Hit Rate: {hitRate:P2}");
                Console.WriteLine($"    Operations: {typeMetrics.TotalOperations}");
                Console.WriteLine($"    Avg Duration: {typeMetrics.AverageOperationDuration.TotalMilliseconds:F2}ms");
            }
        }

        // Show recommendations
        if (updatedMetrics.RecommendedActions.Any())
        {
            Console.WriteLine("\n=== Performance Recommendations ===");
            foreach (var recommendation in updatedMetrics.RecommendedActions)
            {
                Console.WriteLine($"  • {recommendation}");
            }
        }

        // Start periodic metrics reporting (would run in background in production)
        Console.WriteLine("\n=== Starting Background Metrics Reporting ===");
        var cts = new CancellationTokenSource();
        var reportingTask = metricsService.StartPeriodicReporting(TimeSpan.FromSeconds(10), cts.Token);
        
        // Simulate some activity
        Task.Delay(TimeSpan.FromSeconds(2)).Wait();
        
        // Stop reporting and dispose
        cts.Cancel();
        
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Console.WriteLine("Advanced caching demonstration completed!");
    }
}