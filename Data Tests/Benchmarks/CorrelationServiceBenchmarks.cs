using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using MedjCap.Data.Services;
using MedjCap.Data.Tests.Helpers;

namespace MedjCap.Data.Tests.Benchmarks;

/// <summary>
/// Performance benchmarks for CorrelationService operations across different data sizes.
/// Measures correlation calculation performance, outlier detection impact, and memory allocation.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CorrelationServiceBenchmarks
{
    private ICorrelationService _correlationService = null!;
    private ICorrelationService _correlationServiceWithOutliers = null!;
    private List<PriceMovement> _smallDataset = null!;
    private List<PriceMovement> _mediumDataset = null!;
    private List<PriceMovement> _largeDataset = null!;
    private List<PriceMovement> _extraLargeDataset = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        // Create correlation service without outlier detection
        var configWithoutOutliers = TestConfigurationHelper.CreateDefaultStatisticalConfig();
        configWithoutOutliers.Value.OutlierDetection.EnableOutlierDetection = false;
        _correlationService = new CorrelationService(configWithoutOutliers);
        
        // Create correlation service with outlier detection enabled
        var configWithOutliers = TestConfigurationHelper.CreateDefaultStatisticalConfig();
        configWithOutliers.Value.OutlierDetection.EnableOutlierDetection = true;
        _correlationServiceWithOutliers = new CorrelationService(configWithOutliers);
        
        // Generate test datasets of varying sizes
        _smallDataset = GenerateTestData(100);
        _mediumDataset = GenerateTestData(1000);
        _largeDataset = GenerateTestData(10000);
        _extraLargeDataset = GenerateTestData(50000);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(50000)]
    public CorrelationResult CalculateCorrelation_VariableSizes(int dataSize)
    {
        var dataset = GetDatasetBySize(dataSize);
        return _correlationService.CalculateCorrelation(dataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Small_NoOutlierDetection()
    {
        return _correlationService.CalculateCorrelation(_smallDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Small_WithOutlierDetection()
    {
        return _correlationServiceWithOutliers.CalculateCorrelation(_smallDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Medium_NoOutlierDetection()
    {
        return _correlationService.CalculateCorrelation(_mediumDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Medium_WithOutlierDetection()
    {
        return _correlationServiceWithOutliers.CalculateCorrelation(_mediumDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Large_NoOutlierDetection()
    {
        return _correlationService.CalculateCorrelation(_largeDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Large_WithOutlierDetection()
    {
        return _correlationServiceWithOutliers.CalculateCorrelation(_largeDataset, CorrelationType.Pearson);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_Spearman()
    {
        return _correlationService.CalculateCorrelation(_mediumDataset, CorrelationType.Spearman);
    }

    [Benchmark]
    public CorrelationResult CalculateCorrelation_KendallTau()
    {
        return _correlationService.CalculateCorrelation(_mediumDataset, CorrelationType.KendallTau);
    }

    /// <summary>
    /// Benchmark price movement calculation which is often the preprocessing step
    /// </summary>
    [Benchmark]
    public List<PriceMovement> CalculatePriceMovements()
    {
        var timeSeries = GenerateTimeSeriesData(1000);
        return _correlationService.CalculatePriceMovements(timeSeries, TimeSpan.FromMinutes(15));
    }

    private List<PriceMovement> GetDatasetBySize(int size)
    {
        return size switch
        {
            100 => _smallDataset,
            1000 => _mediumDataset,
            10000 => _largeDataset,
            50000 => _extraLargeDataset,
            _ => _mediumDataset
        };
    }

    private List<PriceMovement> GenerateTestData(int count)
    {
        var random = new Random(42); // Fixed seed for reproducible benchmarks
        var movements = new List<PriceMovement>();
        var startTime = DateTime.UtcNow.AddDays(-count);

        for (int i = 0; i < count; i++)
        {
            // Generate realistic financial data with some correlation
            var measurementValue = (decimal)(random.NextDouble() * 100 + 50); // 50-150 range
            var correlation = Math.Sin((double)measurementValue * 0.1); // Introduce some correlation
            var noise = (random.NextDouble() - 0.5) * 2; // -1 to 1 noise
            var atrMovement = (decimal)(correlation * 5 + noise); // Correlated ATR movement

            // Add some outliers (5% of data)
            if (random.NextDouble() < 0.05)
            {
                measurementValue *= (decimal)(1 + (random.NextDouble() - 0.5) * 4); // ±200% outlier
                atrMovement *= (decimal)(1 + (random.NextDouble() - 0.5) * 6); // ±300% outlier
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

    private TimeSeriesData GenerateTimeSeriesData(int count)
    {
        var random = new Random(42);
        var dataPoints = new List<DataPoint>();
        var startTime = DateTime.UtcNow.AddDays(-count);
        var price = 1000m;

        for (int i = 0; i < count; i++)
        {
            // Generate realistic price movement
            var priceChange = (decimal)((random.NextDouble() - 0.5) * 4); // -2 to +2 price movement
            price += priceChange;

            dataPoints.Add(new DataPoint
            {
                Timestamp = startTime.AddMinutes(i),
                Price = price,
                ATR = (decimal)(random.NextDouble() * 2 + 1), // 1-3 ATR range
                MeasurementId = "BENCHMARK",
                MeasurementValue = price
            });
        }

        return new TimeSeriesData
        {
            DataPoints = dataPoints
        };
    }
}