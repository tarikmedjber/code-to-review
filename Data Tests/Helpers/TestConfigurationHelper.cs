using MedjCap.Data.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Tests.Helpers;

/// <summary>
/// Helper class to provide default configurations for tests
/// </summary>
public static class TestConfigurationHelper
{
    public static IOptions<StatisticalConfig> CreateDefaultStatisticalConfig()
    {
        return Options.Create(new StatisticalConfig());
    }

    public static IOptions<OptimizationConfig> CreateDefaultOptimizationConfig()
    {
        return Options.Create(new OptimizationConfig());
    }

    public static IOptions<ValidationConfig> CreateDefaultValidationConfig()
    {
        return Options.Create(new ValidationConfig());
    }

    public static IOptions<CachingConfig> CreateDefaultCachingConfig()
    {
        return Options.Create(new CachingConfig());
    }

    public static IOptions<CachingConfig> CreateDisabledCachingConfig()
    {
        return Options.Create(new CachingConfig { EnableCaching = false });
    }

    public static IOptions<StatisticalConfig> CreateStatisticalConfig(Action<StatisticalConfig> configure)
    {
        var config = new StatisticalConfig();
        configure(config);
        return Options.Create(config);
    }

    public static IOptions<OptimizationConfig> CreateOptimizationConfig(Action<OptimizationConfig> configure)
    {
        var config = new OptimizationConfig();
        configure(config);
        return Options.Create(config);
    }

    public static IOptions<ValidationConfig> CreateValidationConfig(Action<ValidationConfig> configure)
    {
        var config = new ValidationConfig();
        configure(config);
        return Options.Create(config);
    }

    public static IOptions<CachingConfig> CreateCachingConfig(Action<CachingConfig> configure)
    {
        var config = new CachingConfig();
        configure(config);
        return Options.Create(config);
    }
}