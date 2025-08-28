using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Services;
using MedjCap.Data.Services.OptimizationStrategies;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Benchmarks;

/// <summary>
/// Memory allocation and GC pressure benchmarks for MedjCap.Data operations.
/// Focuses on memory efficiency and garbage collection impact of core algorithms.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MemoryAllocationBenchmarks
{
    private ICorrelationService _correlationService = null!;
    private IMLBoundaryOptimizer _optimizer = null!;
    private IOutlierDetectionService _outlierService = null!;
    private List<PriceMovement> _testDataset = null!;
    private MLOptimizationConfig _optimizationConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        _correlationService = new CorrelationService(TestConfigurationHelper.CreateDefaultStatisticalConfig());
        
        var strategyFactory = new OptimizationStrategyFactory(TestConfigurationHelper.CreateDefaultOptimizationConfig());
        _optimizer = new MLBoundaryOptimizer(TestConfigurationHelper.CreateDefaultOptimizationConfig(), strategyFactory);
        
        _outlierService = new OutlierDetectionService(TestConfigurationHelper.CreateDefaultStatisticalConfig());

        // Generate test dataset optimized for memory testing
        _testDataset = GenerateMemoryTestData(2000);

        _optimizationConfig = new MLOptimizationConfig
        {
            UseDecisionTree = true,
            UseClustering = false, // Disable for memory focus
            UseGradientSearch = false,
            TargetATRMove = 2.0m,
            MaxRanges = 3,
            ValidationRatio = 0.2,
            MaxIterations = 50 // Reduced for memory testing
        };
    }

    [Benchmark]
    public CorrelationResult CorrelationCalculation_MemoryEfficient()
    {
        return _correlationService.CalculateCorrelation(_testDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public List<PriceMovement> PriceMovementCalculation_LargeDataset()
    {
        var timeSeries = GenerateTimeSeriesForMemoryTest(5000);
        return _correlationService.CalculatePriceMovements(timeSeries, TimeSpan.FromMinutes(15));
    }

    [Benchmark]
    public OutlierDetectionResult OutlierDetection_MemoryOptimized()
    {
        return _outlierService.DetectOutliers(_testDataset, OutlierDetectionMethod.IQR);
    }

    [Benchmark]
    public List<OptimalBoundary> BoundaryOptimization_MemoryConstrained()
    {
        return _optimizer.FindOptimalBoundaries(_testDataset, 2.0m, 3);
    }

    /// <summary>
    /// Test memory allocation patterns during data processing pipelines
    /// </summary>
    [Benchmark]
    public CorrelationResult DataProcessingPipeline_WithOutlierHandling()
    {
        // This simulates a complete data processing pipeline:
        // 1. Load data (already in memory)
        // 2. Detect and handle outliers
        // 3. Calculate correlation
        // 4. Return results

        var outlierResult = _outlierService.DetectOutliers(_testDataset, OutlierDetectionMethod.IQR);
        var cleanedData = _outlierService.HandleOutliers(_testDataset, outlierResult, OutlierHandlingStrategy.Cap);
        return _correlationService.CalculateCorrelation(cleanedData, CorrelationType.Pearson);
    }

    /// <summary>
    /// Test memory usage during large data aggregation
    /// </summary>
    [Benchmark]
    public Dictionary<TimeSpan, List<PriceMovement>> MultiHorizonCalculation_MemoryImpact()
    {
        var timeSeries = GenerateTimeSeriesForMemoryTest(3000);
        var horizons = new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30) };
        return _correlationService.CalculatePriceMovements(timeSeries, horizons);
    }

    /// <summary>
    /// Test memory allocation during iterative optimization algorithms
    /// </summary>
    [Benchmark]
    public CombinedOptimizationResult IterativeOptimization_MemoryGrowth()
    {
        return _optimizer.RunCombinedOptimization(_testDataset, _optimizationConfig);
    }

    /// <summary>
    /// Stress test: Multiple correlation calculations to test memory cleanup
    /// </summary>
    [Benchmark]
    public List<CorrelationResult> MultipleCorrelations_MemoryCleanup()
    {
        var results = new List<CorrelationResult>();
        
        // Perform multiple calculations to test GC behavior
        for (int i = 0; i < 10; i++)
        {
            var subset = _testDataset.Skip(i * 100).Take(500).ToList();
            if (subset.Count > 50) // Ensure sufficient data
            {
                results.Add(_correlationService.CalculateCorrelation(subset, CorrelationType.Pearson));
            }
        }
        
        return results;
    }

    /// <summary>
    /// Test memory efficiency of data transformation operations
    /// </summary>
    [Benchmark]
    public List<PriceMovement> DataTransformation_InPlace()
    {
        // Test in-place transformations vs creating new objects
        var transformed = new List<PriceMovement>(_testDataset.Count);
        
        foreach (var movement in _testDataset)
        {
            // Simulate some transformation that might cause allocation
            var transformedMovement = movement with 
            { 
                MeasurementValue = movement.MeasurementValue * 1.01m,
                ATRMovement = movement.ATRMovement * 0.99m 
            };
            transformed.Add(transformedMovement);
        }
        
        return transformed;
    }

    /// <summary>
    /// Test memory allocation patterns in LINQ operations
    /// </summary>
    [Benchmark]
    public (double Mean, double StdDev) LinqAggregation_MemoryEfficiency()
    {
        // Test memory efficiency of common LINQ aggregations
        var measurementValues = _testDataset.Select(m => (double)m.MeasurementValue);
        var atrValues = _testDataset.Select(m => (double)m.ATRMovement);
        
        var measurementMean = measurementValues.Average();
        var atrMean = atrValues.Average();
        
        var measurementStdDev = Math.Sqrt(measurementValues.Select(x => Math.Pow(x - measurementMean, 2)).Average());
        var atrStdDev = Math.Sqrt(atrValues.Select(x => Math.Pow(x - atrMean, 2)).Average());
        
        return (measurementMean + atrMean, measurementStdDev + atrStdDev);
    }

    private List<PriceMovement> GenerateMemoryTestData(int count)
    {
        var random = new Random(42);
        var movements = new List<PriceMovement>(count); // Pre-allocate capacity
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            movements.Add(new PriceMovement
            {
                StartTimestamp = startTime.AddMinutes(i * 15),
                MeasurementValue = (decimal)(random.NextDouble() * 100 + 50),
                ATRMovement = (decimal)((random.NextDouble() - 0.5) * 4),
                ContextualData = new Dictionary<string, decimal>() // Empty dictionary to test allocation
            });
        }

        return movements;
    }

    private TimeSeriesData GenerateTimeSeriesForMemoryTest(int count)
    {
        var random = new Random(42);
        var dataPoints = new List<DataPoint>(count); // Pre-allocate
        var startTime = DateTime.UtcNow;
        var price = 1000m;

        for (int i = 0; i < count; i++)
        {
            price += (decimal)((random.NextDouble() - 0.5) * 2);
            
            dataPoints.Add(new DataPoint
            {
                Timestamp = startTime.AddMinutes(i),
                Price = Math.Max(1, price),
                ATR = (decimal)(random.NextDouble() * 2 + 1),
                MeasurementId = "MEM_TEST",
                MeasurementValue = price
            });
        }

        return new TimeSeriesData
        {
            DataPoints = dataPoints
        };
    }
}