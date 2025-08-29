# MedjCap.Data Dependency Injection Configuration

This directory contains the dependency injection configuration for MedjCap.Data analysis services. The DI container provides proper service lifetimes, dependency resolution, and configuration management for all analysis components.

## Quick Start

### Basic Usage with Default Settings

```csharp
using MedjCap.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create service provider with default settings
var serviceProvider = MedjCapServicesFactory.CreateServiceProvider();

// Get analysis services
var correlationService = serviceProvider.GetService<ICorrelationService>();
var mlOptimizer = serviceProvider.GetService<IMLBoundaryOptimizer>();
var analysisEngine = serviceProvider.GetService<IAnalysisEngine>();
```

### Using Service Factory for Simplified Access

```csharp
// Create a service factory
var factory = MedjCapServicesFactory.CreateAnalysisServiceFactory();

// Create services as needed
var correlationService = factory.CreateCorrelationService();
var mlOptimizer = factory.CreateMLBoundaryOptimizer();
var dataCollector = factory.CreateDataCollector();
```

### Quick Service Creation for Simple Scenarios

```csharp
// Direct service creation with defaults
var correlationService = MedjCapServicesFactory.CreateCorrelationService();
var mlOptimizer = MedjCapServicesFactory.CreateMLBoundaryOptimizer();
var dataCollector = MedjCapServicesFactory.CreateDataCollector();
```

## Advanced Configuration

### Custom Configuration Options

```csharp
var serviceProvider = MedjCapServicesFactory.CreateServiceProvider(options =>
{
    // Configure statistical parameters
    options.StatisticalConfig.CorrelationStrengthThresholds.Strong = 0.8m;
    options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
    options.StatisticalConfig.OutlierDetection.ZScoreThreshold = 2.5;
    
    // Configure ML optimization
    options.OptimizationConfig.DefaultQuantileRanges.NumberOfRanges = 10;
    options.OptimizationConfig.FeatureImportance.EnableImportanceAnalysis = true;
    
    // Configure validation
    options.ValidationConfig.DefaultKFolds = 5;
    options.ValidationConfig.EnableCrossValidation = true;
    
    // Enable detailed logging
    options.EnableDetailedLogging = true;
    options.ValidationStrictness = ValidationStrictness.Production;
});
```

### Production Setup with Caching

```csharp
var serviceProvider = MedjCapServicesFactory.CreateCachedServiceProvider(options =>
{
    // Enable caching with custom configuration
    options.EnableCaching = true;
    options.CachingConfig.CorrelationCache.DefaultExpirationMinutes = 30;
    options.CachingConfig.OptimizationCache.DefaultExpirationMinutes = 60;
    options.CachingConfig.StatisticalCache.DefaultExpirationMinutes = 15;
    
    // Production validation settings
    options.ValidationStrictness = ValidationStrictness.Production;
    options.EnableDetailedLogging = true;
});

// Cached services will automatically use intelligent caching
var cachedCorrelationService = serviceProvider.GetService<ICorrelationService>();
var cachedMLOptimizer = serviceProvider.GetService<IMLBoundaryOptimizer>();
```

## Integration with Existing Applications

### ASP.NET Core Integration

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add MedjCap.Data services to existing DI container
    services.AddMedjCapDataServices(options =>
    {
        // Configure as needed
        options.EnableCaching = true;
        options.ValidationStrictness = ValidationStrictness.Production;
    });
    
    // Or use cached version for production
    services.AddMedjCapDataServicesWithCaching(options =>
    {
        options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
        options.OptimizationConfig.DefaultQuantileRanges.NumberOfRanges = 8;
    });
}

// In your controllers or services
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisEngine _analysisEngine;
    
    public AnalysisController(IAnalysisEngine analysisEngine)
    {
        _analysisEngine = analysisEngine;
    }
    
    [HttpPost("analyze")]
    public async Task<AnalysisResult> Analyze(AnalysisRequest request)
    {
        return await _analysisEngine.RunAnalysisAsync(request);
    }
}
```

### Console Application Integration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MedjCap.Data.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Add MedjCap.Data services
builder.Services.AddMedjCapDataServicesWithCaching(options =>
{
    options.StatisticalConfig.OutlierDetection.EnableOutlierDetection = true;
    options.EnableDetailedLogging = true;
});

var app = builder.Build();

// Use services
var analysisEngine = app.Services.GetRequiredService<IAnalysisEngine>();

// Collect some data
analysisEngine.CollectDataPoint(DateTime.Now, "TestIndicator", 75m, 100m, 2.5m);

// Run analysis
var request = new AnalysisRequest 
{ 
    TimeHorizons = new[] { TimeSpan.FromMinutes(15) }
};
var result = await analysisEngine.RunAnalysisAsync(request);
```

## Service Lifetimes

The DI container uses the following service lifetimes:

- **Singleton**: Storage services (ITimeSeriesDataStorage, IAnalysisRepository)
- **Scoped**: Analysis services (ICorrelationService, IMLBoundaryOptimizer, IAnalysisEngine, etc.)
- **Transient**: Not used (all services maintain state or are expensive to create)

This ensures proper resource management while providing good performance characteristics.

## Configuration Classes

- **MedjCapDataOptions**: Main configuration class containing all service options
- **StatisticalConfig**: Statistical analysis parameters and thresholds
- **OptimizationConfig**: ML optimization algorithm parameters
- **ValidationConfig**: Cross-validation and testing parameters  
- **CachingConfig**: Caching behavior and expiration settings

## Factory Classes

- **MedjCapAnalysisServiceFactory**: Instance-based factory for creating services
- **MedjCapServicesFactory**: Static factory for quick service creation
- **ServiceCollectionExtensions**: Extension methods for DI container registration

## Best Practices

1. **Use caching in production** - Enable `AddMedjCapDataServicesWithCaching` for better performance
2. **Configure validation strictness** - Use `ValidationStrictness.Production` in production environments
3. **Customize statistical parameters** - Adjust thresholds and parameters based on your data characteristics
4. **Enable logging in development** - Set `EnableDetailedLogging = true` for debugging
5. **Scope services properly** - Use scoped lifetime in web applications, singleton in console apps
6. **Monitor cache performance** - Check cache hit rates and adjust expiration times as needed

## Testing

For unit testing, you can create lightweight service providers:

```csharp
[Test]
public void TestCorrelationAnalysis()
{
    // Create test service provider
    var serviceProvider = MedjCapServicesFactory.CreateServiceProvider(options =>
    {
        options.ValidationStrictness = ValidationStrictness.Minimal;
        options.EnableCaching = false; // Disable caching in tests
    });
    
    var correlationService = serviceProvider.GetRequiredService<ICorrelationService>();
    
    // Run your test
    var result = correlationService.CalculateCorrelation(testData, CorrelationType.Pearson);
    Assert.IsNotNull(result);
}
```