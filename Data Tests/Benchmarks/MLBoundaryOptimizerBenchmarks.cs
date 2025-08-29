using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.MachineLearning.Services;
using MedjCap.Data.MachineLearning.Services.OptimizationStrategies;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Benchmarks;

/// <summary>
/// Performance benchmarks for ML boundary optimization algorithms.
/// Compares Strategy pattern performance against different dataset sizes and algorithm types.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MLBoundaryOptimizerBenchmarks
{
    private IMLBoundaryOptimizer _optimizer = null!;
    private List<PriceMovement> _smallDataset = null!;
    private List<PriceMovement> _mediumDataset = null!;
    private List<PriceMovement> _largeDataset = null!;
    private MLOptimizationConfig _optimizationConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var strategyFactory = new OptimizationStrategyFactory(TestConfigurationHelper.CreateDefaultOptimizationConfig());
        _optimizer = new MLBoundaryOptimizer(TestConfigurationHelper.CreateDefaultOptimizationConfig(), strategyFactory);
        
        // Generate test datasets
        _smallDataset = GenerateTestData(200);
        _mediumDataset = GenerateTestData(1000);
        _largeDataset = GenerateTestData(5000);

        _optimizationConfig = new MLOptimizationConfig
        {
            UseDecisionTree = true,
            UseClustering = true,
            UseGradientSearch = true,
            TargetATRMove = 2.0m,
            MaxRanges = 5,
            ValidationRatio = 0.2,
            MaxIterations = 100,
            ConvergenceThreshold = 0.001,
            AlgorithmParameters = new Dictionary<string, object>
            {
                ["DecisionTreeMaxDepth"] = 5,
                ["DecisionTreeMinSamplesPerLeaf"] = 10,
                ["ClusterCount"] = 5,
                ["ClusterMaxIterations"] = 100,
                ["GradientMaxIterations"] = 50,
                ["GradientLearningRate"] = 0.01
            }
        };
    }

    [Benchmark]
    public List<OptimalBoundary> FindOptimalBoundaries_Small()
    {
        return _optimizer.FindOptimalBoundaries(_smallDataset, 2.0m, 3);
    }

    [Benchmark]
    public List<OptimalBoundary> FindOptimalBoundaries_Medium()
    {
        return _optimizer.FindOptimalBoundaries(_mediumDataset, 2.0m, 5);
    }

    [Benchmark]
    public List<OptimalBoundary> FindOptimalBoundaries_Large()
    {
        return _optimizer.FindOptimalBoundaries(_largeDataset, 2.0m, 5);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_Small()
    {
        return _optimizer.RunCombinedOptimization(_smallDataset, _optimizationConfig);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_Medium()
    {
        return _optimizer.RunCombinedOptimization(_mediumDataset, _optimizationConfig);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_Large()
    {
        return _optimizer.RunCombinedOptimization(_largeDataset, _optimizationConfig);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_DecisionTreeOnly()
    {
        var config = _optimizationConfig with
        {
            UseDecisionTree = true,
            UseClustering = false,
            UseGradientSearch = false
        };
        return _optimizer.RunCombinedOptimization(_mediumDataset, config);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_ClusteringOnly()
    {
        var config = _optimizationConfig with
        {
            UseDecisionTree = false,
            UseClustering = true,
            UseGradientSearch = false
        };
        return _optimizer.RunCombinedOptimization(_mediumDataset, config);
    }

    [Benchmark]
    public CombinedOptimizationResult RunCombinedOptimization_GradientSearchOnly()
    {
        var config = _optimizationConfig with
        {
            UseDecisionTree = false,
            UseClustering = false,
            UseGradientSearch = true
        };
        return _optimizer.RunCombinedOptimization(_mediumDataset, config);
    }

    private List<PriceMovement> GenerateTestData(int count)
    {
        var random = new Random(42); // Fixed seed for reproducible benchmarks
        var movements = new List<PriceMovement>();
        var startTime = DateTime.UtcNow.AddDays(-count / 96); // 15-min intervals

        for (int i = 0; i < count; i++)
        {
            // Create realistic measurement patterns with multiple ranges
            var measurementValue = GenerateRealisticMeasurement(i, count, random);
            var atrMovement = GenerateCorrelatedATRMovement(measurementValue, random);

            movements.Add(new PriceMovement
            {
                StartTimestamp = startTime.AddMinutes(i * 15),
                MeasurementValue = measurementValue,
                ATRMovement = atrMovement
            });
        }

        return movements;
    }

    private decimal GenerateRealisticMeasurement(int index, int totalCount, Random random)
    {
        // Create multiple distinct ranges with different characteristics
        var progress = (double)index / totalCount;
        
        // Base pattern: sine wave with trend
        var baseValue = Math.Sin(progress * 8 * Math.PI) * 20 + progress * 50 + 50;
        
        // Add some zones with higher/lower values
        if (progress > 0.2 && progress < 0.4)
            baseValue += 30; // High zone
        else if (progress > 0.6 && progress < 0.8)
            baseValue -= 20; // Low zone
            
        // Add noise
        var noise = (random.NextDouble() - 0.5) * 10;
        
        return (decimal)Math.Max(1, baseValue + noise);
    }

    private decimal GenerateCorrelatedATRMovement(decimal measurementValue, Random random)
    {
        // Create correlation with measurement value in certain ranges
        var correlation = 0.0;
        
        if (measurementValue > 80 && measurementValue < 120)
            correlation = 0.7; // Strong positive correlation in this range
        else if (measurementValue < 30)
            correlation = -0.5; // Negative correlation for low values
        else
            correlation = 0.1; // Weak correlation elsewhere
        
        var baseMovement = correlation * (double)measurementValue * 0.05;
        var noise = (random.NextDouble() - 0.5) * 2;
        
        return (decimal)(baseMovement + noise);
    }
}