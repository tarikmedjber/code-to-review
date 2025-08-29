using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.Trading.Models;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.MachineLearning.Services.OptimizationStrategies;

/// <summary>
/// Factory for creating optimization strategies based on configuration.
/// Implements the Strategy pattern for ML boundary optimization algorithms.
/// </summary>
public class OptimizationStrategyFactory : IOptimizationStrategyFactory
{
    private readonly IOptions<OptimizationConfig> _optimizationConfig;

    public OptimizationStrategyFactory(IOptions<OptimizationConfig> optimizationConfig)
    {
        _optimizationConfig = optimizationConfig ?? throw new ArgumentNullException(nameof(optimizationConfig));
    }

    /// <summary>
    /// Creates all available optimization strategies based on configuration.
    /// </summary>
    public IEnumerable<IOptimizationStrategy> CreateStrategies(MLOptimizationConfig config)
    {
        var strategies = new List<IOptimizationStrategy>();

        // Create all available strategies
        var allStrategies = new IOptimizationStrategy[]
        {
            new DecisionTreeOptimizationStrategy(_optimizationConfig, Options.Create(config)),
            new ClusteringOptimizationStrategy(_optimizationConfig, Options.Create(config)),
            new GradientSearchOptimizationStrategy(_optimizationConfig, Options.Create(config))
        };

        // Return only enabled strategies
        return allStrategies.Where(s => s.IsEnabled).ToList();
    }

    /// <summary>
    /// Creates a specific optimization strategy by name.
    /// </summary>
    public IOptimizationStrategy? CreateStrategy(string strategyName, MLOptimizationConfig config)
    {
        return strategyName?.ToLowerInvariant() switch
        {
            "decisiontree" => new DecisionTreeOptimizationStrategy(_optimizationConfig, Options.Create(config)),
            "clustering" => new ClusteringOptimizationStrategy(_optimizationConfig, Options.Create(config)),
            "gradientsearch" => new GradientSearchOptimizationStrategy(_optimizationConfig, Options.Create(config)),
            _ => null
        };
    }

    /// <summary>
    /// Gets names of all supported optimization strategies.
    /// </summary>
    public IEnumerable<string> GetSupportedStrategies()
    {
        return new[] { "DecisionTree", "Clustering", "GradientSearch" };
    }
}