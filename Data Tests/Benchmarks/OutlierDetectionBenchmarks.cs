using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Services;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Benchmarks;

/// <summary>
/// Performance benchmarks for outlier detection algorithms.
/// Compares different detection methods and handling strategies across various data sizes.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OutlierDetectionBenchmarks
{
    private IOutlierDetectionService _outlierService = null!;
    private List<PriceMovement> _smallDataset = null!;
    private List<PriceMovement> _mediumDataset = null!;
    private List<PriceMovement> _largeDataset = null!;
    private List<PriceMovement> _datasetWithOutliers = null!;

    [GlobalSetup]
    public void Setup()
    {
        _outlierService = new OutlierDetectionService(TestConfigurationHelper.CreateDefaultStatisticalConfig());
        
        // Generate test datasets with varying outlier characteristics
        _smallDataset = GenerateTestData(100, 0.05);
        _mediumDataset = GenerateTestData(1000, 0.08);
        _largeDataset = GenerateTestData(10000, 0.1);
        _datasetWithOutliers = GenerateTestData(1000, 0.15); // High outlier ratio
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_IQR_Small()
    {
        return _outlierService.DetectOutliers(_smallDataset, OutlierDetectionMethod.IQR);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_IQR_Medium()
    {
        return _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.IQR);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_IQR_Large()
    {
        return _outlierService.DetectOutliers(_largeDataset, OutlierDetectionMethod.IQR);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_ZScore_Medium()
    {
        return _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.ZScore);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_ModifiedZScore_Medium()
    {
        return _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.ModifiedZScore);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_IsolationForest_Medium()
    {
        return _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.IsolationForest);
    }

    [Benchmark]
    public OutlierDetectionResult DetectOutliers_Ensemble_Medium()
    {
        return _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.Ensemble);
    }

    [Benchmark]
    public OutlierAnalysisResult AnalyzeOutliers_Comprehensive()
    {
        return _outlierService.AnalyzeOutliers(_mediumDataset);
    }

    [Benchmark]
    public List<PriceMovement> HandleOutliers_Remove()
    {
        var detectionResult = _outlierService.DetectOutliers(_datasetWithOutliers, OutlierDetectionMethod.IQR);
        return _outlierService.HandleOutliers(_datasetWithOutliers, detectionResult, OutlierHandlingStrategy.Remove);
    }

    [Benchmark]
    public List<PriceMovement> HandleOutliers_Cap()
    {
        var detectionResult = _outlierService.DetectOutliers(_datasetWithOutliers, OutlierDetectionMethod.IQR);
        return _outlierService.HandleOutliers(_datasetWithOutliers, detectionResult, OutlierHandlingStrategy.Cap);
    }

    [Benchmark]
    public List<PriceMovement> HandleOutliers_ReplaceWithMedian()
    {
        var detectionResult = _outlierService.DetectOutliers(_datasetWithOutliers, OutlierDetectionMethod.IQR);
        return _outlierService.HandleOutliers(_datasetWithOutliers, detectionResult, OutlierHandlingStrategy.ReplaceWithMedian);
    }

    [Benchmark]
    public List<PriceMovement> HandleOutliers_LogTransform()
    {
        var detectionResult = _outlierService.DetectOutliers(_datasetWithOutliers, OutlierDetectionMethod.IQR);
        return _outlierService.HandleOutliers(_datasetWithOutliers, detectionResult, OutlierHandlingStrategy.LogTransform);
    }

    [Benchmark]
    public DataQualityReport AssessDataQuality()
    {
        return _outlierService.AssessDataQuality(_mediumDataset);
    }

    /// <summary>
    /// Benchmark the entire outlier detection pipeline: detect + handle
    /// </summary>
    [Benchmark]
    public List<PriceMovement> CompleteOutlierPipeline_IQR_Cap()
    {
        var detectionResult = _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.IQR);
        return _outlierService.HandleOutliers(_mediumDataset, detectionResult, OutlierHandlingStrategy.Cap);
    }

    [Benchmark]
    public List<PriceMovement> CompleteOutlierPipeline_Ensemble_Remove()
    {
        var detectionResult = _outlierService.DetectOutliers(_mediumDataset, OutlierDetectionMethod.Ensemble);
        return _outlierService.HandleOutliers(_mediumDataset, detectionResult, OutlierHandlingStrategy.Remove);
    }

    private List<PriceMovement> GenerateTestData(int count, double outlierRatio)
    {
        var random = new Random(42); // Fixed seed for reproducible benchmarks
        var movements = new List<PriceMovement>();
        var startTime = DateTime.UtcNow.AddDays(-count / 96); // 15-min intervals

        for (int i = 0; i < count; i++)
        {
            // Generate normal data with realistic financial patterns
            var measurementValue = GenerateNormalMeasurement(i, random);
            var atrMovement = GenerateNormalATRMovement(measurementValue, random);

            // Inject outliers based on specified ratio
            if (random.NextDouble() < outlierRatio)
            {
                var outlierType = random.Next(3);
                switch (outlierType)
                {
                    case 0: // Extreme high measurement
                        measurementValue *= (decimal)(3 + random.NextDouble() * 2); // 3x-5x multiplier
                        break;
                    case 1: // Extreme low measurement  
                        measurementValue /= (decimal)(3 + random.NextDouble() * 2);
                        break;
                    case 2: // Extreme ATR movement
                        atrMovement *= (decimal)(4 + random.NextDouble() * 3); // 4x-7x multiplier
                        if (random.NextDouble() < 0.5) atrMovement = -atrMovement;
                        break;
                }
            }

            movements.Add(new PriceMovement
            {
                StartTimestamp = startTime.AddMinutes(i * 15),
                MeasurementValue = measurementValue,
                ATRMovement = atrMovement
            });
        }

        return movements;
    }

    private decimal GenerateNormalMeasurement(int index, Random random)
    {
        // Generate measurements following a normal-ish distribution around 50-100 range
        var baseValue = 75; // Center around 75
        var variation = (random.NextGaussian() * 15); // Standard deviation of 15
        return (decimal)Math.Max(1, baseValue + variation);
    }

    private decimal GenerateNormalATRMovement(decimal measurementValue, Random random)
    {
        // Generate ATR movements with slight correlation to measurement value
        var correlation = (double)(measurementValue - 75) * 0.02; // Weak correlation
        var noise = random.NextGaussian() * 2; // Random component
        return (decimal)(correlation + noise);
    }
}

/// <summary>
/// Extension method for generating Gaussian-distributed random numbers
/// </summary>
public static class RandomExtensions
{
    public static double NextGaussian(this Random random)
    {
        // Box-Muller transform for Gaussian distribution
        static double NextGaussianInternal(Random r)
        {
            double u1 = 1.0 - r.NextDouble();
            double u2 = 1.0 - r.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
        
        return NextGaussianInternal(random);
    }
}