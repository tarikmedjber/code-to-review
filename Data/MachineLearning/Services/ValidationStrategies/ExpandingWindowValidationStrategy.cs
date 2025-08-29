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
/// Time-series cross-validation with expanding training windows.
/// Respects temporal order and uses progressively larger training sets.
/// </summary>
public class ExpandingWindowValidationStrategy : IValidationStrategy
{
    private readonly StatisticalConfig _statisticalConfig;
    
    public string StrategyName => "Expanding Window Cross-Validation";
    public CrossValidationConfig Config { get; }

    public ExpandingWindowValidationStrategy(CrossValidationConfig config, StatisticalConfig statisticalConfig)
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
        
        var windows = CreateExpandingWindows(sortedData);
        var foldResults = new List<CrossValidationFold>();
        var foldScores = new List<double>();

        for (int i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            var trainData = window.TrainingData;
            var testData = window.ValidationData;

            // Train on expanding window
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

    private List<TimeSeriesWindow> CreateExpandingWindows(List<PriceMovement> sortedData)
    {
        var windows = new List<TimeSeriesWindow>();
        var dataSize = sortedData.Count;
        var minTrainSize = (int)(dataSize * Config.MinimumTrainWindowSize);
        var stepSize = Math.Max(1, (int)(dataSize * Config.StepSize));
        var testSize = Math.Max(1, stepSize); // Test window size equals step size

        // Ensure minimum training size
        if (minTrainSize < 10)
            minTrainSize = Math.Min(10, dataSize / 2);

        for (int trainEnd = minTrainSize; trainEnd + testSize <= dataSize; trainEnd += stepSize)
        {
            var testStart = trainEnd;
            var testEnd = Math.Min(testStart + testSize, dataSize);
            
            var trainingData = sortedData.Take(trainEnd).ToList();
            var validationData = sortedData.Skip(testStart).Take(testEnd - testStart).ToList();

            if (trainingData.Count >= minTrainSize && validationData.Any())
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

        // Calculate temporal degradation (performance decline over time)
        var temporalDegradation = CalculateTemporalDegradation(foldResults);
        
        // Simple stationarity check based on performance variance
        var isStationary = stdDevScore < 0.2 && temporalDegradation < 0.3;
        
        // Estimate optimal lookback window
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
                ["TemporalTrend"] = temporalDegradation
            },
            Metrics = new Dictionary<string, double>
            {
                ["WindowCount"] = foldResults.Count,
                ["AvgTrainingScore"] = avgTrainingScore,
                ["AvgValidationScore"] = avgValidationScore,
                ["TrainValGap"] = avgTrainingScore - avgValidationScore,
                ["TemporalDegradation"] = temporalDegradation,
                ["EstimatedOptimalLookbackDays"] = optimalLookback.TotalDays
            }
        };
    }

    private double CalculateTemporalDegradation(List<CrossValidationFold> foldResults)
    {
        if (foldResults.Count < 2)
            return 0;

        // Calculate the trend in validation scores over time
        var scores = foldResults.Select(f => f.ValidationScore).ToList();
        var n = scores.Count;
        
        // Simple linear regression slope calculation
        var xMean = (n - 1) / 2.0; // Time index mean
        var yMean = scores.Average();
        
        var numerator = 0.0;
        var denominator = 0.0;
        
        for (int i = 0; i < n; i++)
        {
            numerator += (i - xMean) * (scores[i] - yMean);
            denominator += (i - xMean) * (i - xMean);
        }
        
        var slope = denominator > 0 ? numerator / denominator : 0;
        
        // Return absolute degradation (negative slope indicates declining performance)
        return Math.Max(0, -slope);
    }

    private TimeSpan EstimateOptimalLookbackWindow(List<CrossValidationFold> foldResults, List<PriceMovement> sortedData)
    {
        if (foldResults.Count < 2)
            return TimeSpan.FromDays(30); // Default

        // Find the fold with the best validation score
        var bestFold = foldResults.OrderByDescending(f => f.ValidationScore).First();
        var bestTrainingSize = bestFold.TrainingSampleCount;
        
        // Estimate time span based on training size
        var totalTimeSpan = sortedData.Last().StartTimestamp - sortedData.First().StartTimestamp;
        var optimalRatio = (double)bestTrainingSize / sortedData.Count;
        
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

