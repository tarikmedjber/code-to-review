using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.Infrastructure.Interfaces;
using MedjCap.Data.Analysis.Interfaces;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.Statistics.Interfaces;
using MedjCap.Data.DataQuality.Interfaces;
using MedjCap.Data.Backtesting.Interfaces;
using MedjCap.Data.TimeSeries.Interfaces;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.Infrastructure.Models;

namespace MedjCap.Data.MachineLearning.Services.ValidationStrategies;

/// <summary>
/// Time-series cross-validation with rolling (sliding) training windows.
/// Uses fixed-size training windows that slide through the time series.
/// </summary>
public class RollingWindowValidationStrategy : IValidationStrategy
{
    private readonly StatisticalConfig _statisticalConfig;
    
    public string StrategyName => "Rolling Window Cross-Validation";
    public CrossValidationConfig Config { get; }

    public RollingWindowValidationStrategy(CrossValidationConfig config, StatisticalConfig statisticalConfig)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _statisticalConfig = statisticalConfig ?? throw new ArgumentNullException(nameof(statisticalConfig));
    }

    public CrossValidationResult Validate(List<PriceMovement> data, IOptimizationMethod method)
    {
        if (data == null || !data.Any())
            throw new ArgumentNullException(nameof(data));
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        // Sort data by timestamp to ensure temporal order
        var sortedData = data.OrderBy(m => m.StartTimestamp).ToList();
        
        var windows = CreateRollingWindows(sortedData);
        var foldResults = new List<CrossValidationFold>();
        var foldScores = new List<double>();

        for (int i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            var trainData = window.TrainingData;
            var testData = window.ValidationData;

            // Train on rolling window
            var trainingBoundaries = method.Train(trainData, CreateMLConfig());

            // Evaluate on future data
            var validationScore = method.Evaluate(trainingBoundaries, testData, CreateMLConfig());
            var trainingScore = method.Evaluate(trainingBoundaries, trainData, CreateMLConfig());

            // Create validation result for this window
            var validationResult = CreateValidationResult(trainingBoundaries, testData);

            foldResults.Add(new CrossValidationFold
            {
                FoldIndex = i,
                TrainingScore = trainingScore,
                ValidationScore = validationScore,
                TrainingBoundaries = trainingBoundaries,
                ValidationResult = validationResult,
                TrainingSampleCount = trainData.Count,
                ValidationSampleCount = testData.Count,
                Period = new DateRange
                {
                    Start = trainData.First().StartTimestamp,
                    End = testData.Last().StartTimestamp
                }
            });

            foldScores.Add(validationScore);
        }

        return CreateTimeSeriesCrossValidationResult(foldScores, foldResults, sortedData);
    }

    private List<TimeSeriesWindow> CreateRollingWindows(List<PriceMovement> sortedData)
    {
        var windows = new List<TimeSeriesWindow>();
        var dataSize = sortedData.Count;
        var trainSize = (int)(dataSize * Config.MinimumTrainWindowSize);
        var stepSize = Math.Max(1, (int)(dataSize * Config.StepSize));
        var testSize = Math.Max(1, stepSize); // Test window size equals step size

        // Ensure minimum training size
        if (trainSize < 10)
            trainSize = Math.Min(10, dataSize / 3);

        for (int start = 0; start + trainSize + testSize <= dataSize; start += stepSize)
        {
            var trainEnd = start + trainSize;
            var testStart = trainEnd;
            var testEnd = Math.Min(testStart + testSize, dataSize);
            
            var trainingData = sortedData.Skip(start).Take(trainSize).ToList();
            var validationData = sortedData.Skip(testStart).Take(testEnd - testStart).ToList();

            if (trainingData.Count >= trainSize && validationData.Any())
            {
                windows.Add(new TimeSeriesWindow
                {
                    TrainingData = trainingData,
                    ValidationData = validationData,
                    TrainingPeriod = new DateRange
                    {
                        Start = trainingData.First().StartTimestamp,
                        End = trainingData.Last().StartTimestamp
                    },
                    ValidationPeriod = new DateRange
                    {
                        Start = validationData.First().StartTimestamp,
                        End = validationData.Last().StartTimestamp
                    }
                });
            }
        }

        return windows;
    }

    private ValidationResult CreateValidationResult(List<OptimalBoundary> boundaries, List<PriceMovement> testData)
    {
        if (!boundaries.Any() || !testData.Any())
        {
            return new ValidationResult
            {
                InSamplePerformance = 0,
                OutOfSamplePerformance = 0,
                PerformanceDegradation = 1.0,
                IsOverfitted = true
            };
        }

        var boundaryPerformances = new List<BoundaryValidation>();
        double totalOutOfSampleScore = 0;
        double totalWeight = 0;

        foreach (var boundary in boundaries)
        {
            var testMovementsInRange = testData
                .Where(m => m.MeasurementValue >= boundary.RangeLow && m.MeasurementValue <= boundary.RangeHigh)
                .ToList();

            var outOfSampleHitRate = 0.0;
            if (testMovementsInRange.Any())
            {
                var targetATR = boundary.ExpectedATRMove;
                outOfSampleHitRate = (double)testMovementsInRange.Count(m => Math.Abs(m.ATRMovement) >= targetATR) / testMovementsInRange.Count;
            }

            var performanceDegradation = boundary.HitRate > 0 ? Math.Abs(boundary.HitRate - outOfSampleHitRate) / boundary.HitRate : 1.0;
            var isStable = performanceDegradation < 0.3;
            var stabilityScore = Math.Max(0, 1.0 - performanceDegradation);

            boundaryPerformances.Add(new BoundaryValidation
            {
                Boundary = boundary,
                InSampleHitRate = boundary.HitRate,
                OutOfSampleHitRate = outOfSampleHitRate,
                IsStable = isStable,
                StabilityScore = stabilityScore
            });

            var weight = Math.Sqrt(boundary.SampleCount);
            totalOutOfSampleScore += outOfSampleHitRate * weight;
            totalWeight += weight;
        }

        var overallOutOfSamplePerformance = totalWeight > 0 ? totalOutOfSampleScore / totalWeight : 0;
        var overallInSamplePerformance = boundaries.Average(b => b.HitRate);
        var overallDegradation = overallInSamplePerformance > 0 ? Math.Abs(overallInSamplePerformance - overallOutOfSamplePerformance) / overallInSamplePerformance : 1.0;

        return new ValidationResult
        {
            InSamplePerformance = overallInSamplePerformance,
            OutOfSamplePerformance = overallOutOfSamplePerformance,
            PerformanceDegradation = overallDegradation,
            BoundaryPerformance = boundaryPerformances,
            IsOverfitted = overallDegradation > 0.5,
            ValidationMetrics = new Dictionary<string, double>
            {
                ["StableBoundariesPct"] = boundaryPerformances.Any() ? (double)boundaryPerformances.Count(bp => bp.IsStable) / boundaryPerformances.Count : 0,
                ["AverageStabilityScore"] = boundaryPerformances.Any() ? boundaryPerformances.Average(bp => bp.StabilityScore) : 0,
                ["TestSampleSize"] = testData.Count
            }
        };
    }

    private TimeSeriesCrossValidationResult CreateTimeSeriesCrossValidationResult(
        List<double> foldScores, 
        List<CrossValidationFold> foldResults,
        List<PriceMovement> sortedData)
    {
        var meanScore = foldScores.Average();
        var stdDevScore = CalculateStdDev(foldScores);
        
        // Calculate confidence interval
        var confidenceInterval = CalculateConfidenceInterval(foldScores, Config.ConfidenceLevel);
        
        // Detect overfitting and temporal degradation
        var avgTrainingScore = foldResults.Average(f => f.TrainingScore);
        var avgValidationScore = foldResults.Average(f => f.ValidationScore);
        var isOverfitting = avgTrainingScore > avgValidationScore + 0.1;

        // Calculate temporal degradation for rolling windows
        var temporalDegradation = CalculateTemporalDegradation(foldResults);
        
        // Rolling windows tend to be more stable, so adjust stationarity threshold
        var isStationary = stdDevScore < 0.25 && temporalDegradation < 0.2;
        
        // For rolling windows, optimal lookback is the configured window size
        var optimalLookback = EstimateOptimalLookbackWindow(foldResults, sortedData);

        return new TimeSeriesCrossValidationResult
        {
            FoldScores = foldScores,
            MeanScore = meanScore,
            StdDevScore = stdDevScore,
            ConfidenceInterval = confidenceInterval,
            FoldResults = foldResults,
            IsOverfitting = isOverfitting,
            Config = Config,
            IsStationary = isStationary,
            TemporalDegradation = temporalDegradation,
            OptimalLookbackWindow = optimalLookback,
            StationarityTests = new Dictionary<string, double>
            {
                ["PerformanceVariance"] = stdDevScore,
                ["TemporalTrend"] = temporalDegradation,
                ["WindowConsistency"] = CalculateWindowConsistency(foldResults)
            },
            Metrics = new Dictionary<string, double>
            {
                ["WindowCount"] = foldResults.Count,
                ["AvgTrainingScore"] = avgTrainingScore,
                ["AvgValidationScore"] = avgValidationScore,
                ["TrainValGap"] = avgTrainingScore - avgValidationScore,
                ["TemporalDegradation"] = temporalDegradation,
                ["WindowConsistency"] = CalculateWindowConsistency(foldResults),
                ["EstimatedOptimalLookbackDays"] = optimalLookback.TotalDays
            }
        };
    }

    private double CalculateTemporalDegradation(List<CrossValidationFold> foldResults)
    {
        if (foldResults.Count < 2)
            return 0;

        // For rolling windows, look at the variance in performance across windows
        // rather than just the trend, as rolling windows can show more cyclical patterns
        var scores = foldResults.Select(f => f.ValidationScore).ToList();
        var mean = scores.Average();
        var variance = scores.Select(s => Math.Pow(s - mean, 2)).Average();
        
        // Normalize variance to [0,1] scale
        return Math.Min(1.0, Math.Sqrt(variance));
    }

    private double CalculateWindowConsistency(List<CrossValidationFold> foldResults)
    {
        if (foldResults.Count < 2)
            return 1.0;

        // Measure how consistent the performance is across rolling windows
        var scores = foldResults.Select(f => f.ValidationScore).ToList();
        var coefficientOfVariation = CalculateStdDev(scores) / Math.Max(scores.Average(), 0.001);
        
        // Convert to consistency score (lower CV = higher consistency)
        return Math.Max(0, 1.0 - coefficientOfVariation);
    }

    private TimeSpan EstimateOptimalLookbackWindow(List<CrossValidationFold> foldResults, List<PriceMovement> sortedData)
    {
        if (foldResults.Count < 2)
            return TimeSpan.FromDays(30); // Default

        // For rolling windows, the optimal lookback is based on the configured window size
        // that was used, since all windows are the same size
        var avgTrainingSize = foldResults.Average(f => f.TrainingSampleCount);
        var totalTimeSpan = sortedData.Last().StartTimestamp - sortedData.First().StartTimestamp;
        var optimalRatio = avgTrainingSize / sortedData.Count;
        
        return TimeSpan.FromTicks((long)(totalTimeSpan.Ticks * optimalRatio));
    }

    private MLOptimizationConfig CreateMLConfig()
    {
        return new MLOptimizationConfig();
    }

    private double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Select(v => Math.Pow(v - mean, 2)).Sum() / (values.Count - 1);
        return Math.Sqrt(variance);
    }

    private (double Lower, double Upper) CalculateConfidenceInterval(List<double> values, double confidenceLevel)
    {
        if (values.Count < 2)
            return (0, 0);

        var mean = values.Average();
        var stdDev = CalculateStdDev(values);
        var stdError = stdDev / Math.Sqrt(values.Count);
        var margin = 1.96 * stdError; // Approximation for 95% confidence
        
        return (mean - margin, mean + margin);
    }
}