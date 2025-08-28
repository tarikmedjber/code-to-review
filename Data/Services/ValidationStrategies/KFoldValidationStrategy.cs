using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;

namespace MedjCap.Data.Services;

/// <summary>
/// Standard k-fold cross-validation strategy for i.i.d. data.
/// Randomly splits data into k folds and validates using each fold as test set.
/// </summary>
public class KFoldValidationStrategy : IValidationStrategy
{
    private readonly StatisticalConfig _statisticalConfig;
    
    public string StrategyName => "K-Fold Cross-Validation";
    public CrossValidationConfig Config { get; }

    public KFoldValidationStrategy(CrossValidationConfig config, StatisticalConfig statisticalConfig)
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

        var folds = CreateKFolds(data, Config.KFolds, Config.RandomSeed);
        var foldResults = new List<CrossValidationFold>();
        var foldScores = new List<double>();

        for (int i = 0; i < folds.Count; i++)
        {
            var testData = folds[i];
            var trainData = folds.Where((_, idx) => idx != i).SelectMany(f => f).ToList();

            // Train on k-1 folds
            var trainingBoundaries = method.Train(trainData, CreateMLConfig());

            // Evaluate on held-out fold
            var validationScore = method.Evaluate(trainingBoundaries, testData, CreateMLConfig());
            var trainingScore = method.Evaluate(trainingBoundaries, trainData, CreateMLConfig());

            // Create validation result for this fold
            var validationResult = CreateValidationResult(trainingBoundaries, testData);

            foldResults.Add(new CrossValidationFold
            {
                FoldIndex = i,
                TrainingScore = trainingScore,
                ValidationScore = validationScore,
                TrainingBoundaries = trainingBoundaries,
                ValidationResult = validationResult,
                TrainingSampleCount = trainData.Count,
                ValidationSampleCount = testData.Count
            });

            foldScores.Add(validationScore);
        }

        return CreateCrossValidationResult(foldScores, foldResults);
    }

    private List<List<PriceMovement>> CreateKFolds(List<PriceMovement> data, int k, int? randomSeed)
    {
        var shuffledData = data.ToList();
        
        // Shuffle data if random seed provided or no seed (random)
        var random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        for (int i = shuffledData.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffledData[i], shuffledData[j]) = (shuffledData[j], shuffledData[i]);
        }

        var folds = new List<List<PriceMovement>>();
        var foldSize = shuffledData.Count / k;
        var remainder = shuffledData.Count % k;

        int startIndex = 0;
        for (int i = 0; i < k; i++)
        {
            var currentFoldSize = foldSize + (i < remainder ? 1 : 0);
            folds.Add(shuffledData.Skip(startIndex).Take(currentFoldSize).ToList());
            startIndex += currentFoldSize;
        }

        return folds;
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

        // Calculate performance on test data
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
            var isStable = performanceDegradation < 0.3; // Less than 30% degradation
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

    private CrossValidationResult CreateCrossValidationResult(List<double> foldScores, List<CrossValidationFold> foldResults)
    {
        var meanScore = foldScores.Average();
        var stdDevScore = CalculateStdDev(foldScores);
        
        // Calculate confidence interval
        var confidenceInterval = CalculateConfidenceInterval(foldScores, Config.ConfidenceLevel);
        
        // Detect overfitting by comparing training vs validation performance
        var avgTrainingScore = foldResults.Average(f => f.TrainingScore);
        var avgValidationScore = foldResults.Average(f => f.ValidationScore);
        var isOverfitting = avgTrainingScore > avgValidationScore + 0.1; // 10% threshold

        return new CrossValidationResult
        {
            FoldScores = foldScores,
            MeanScore = meanScore,
            StdDevScore = stdDevScore,
            ConfidenceInterval = confidenceInterval,
            FoldResults = foldResults,
            IsOverfitting = isOverfitting,
            Config = Config,
            Metrics = new Dictionary<string, double>
            {
                ["FoldCount"] = Config.KFolds,
                ["AvgTrainingScore"] = avgTrainingScore,
                ["AvgValidationScore"] = avgValidationScore,
                ["TrainValGap"] = avgTrainingScore - avgValidationScore
            }
        };
    }

    private MLOptimizationConfig CreateMLConfig()
    {
        // Create a basic configuration for ML optimization
        // This would typically be passed in or configured
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
        
        // Use t-distribution for small samples
        var alpha = 1 - confidenceLevel;
        var tValue = CalculateTValue(values.Count - 1, alpha / 2);
        
        var margin = tValue * stdError;
        return (mean - margin, mean + margin);
    }

    private double CalculateTValue(int degreesOfFreedom, double alpha)
    {
        // Simplified t-value calculation for common confidence levels
        // For a more precise implementation, would use a proper t-distribution function
        return confidenceLevel switch
        {
            0.95 => degreesOfFreedom switch
            {
                1 => 12.706,
                2 => 4.303,
                3 => 3.182,
                4 => 2.776,
                >= 5 and < 10 => 2.5,
                >= 10 and < 30 => 2.1,
                _ => 1.96
            },
            0.90 => 1.645,
            0.99 => 2.576,
            _ => 1.96
        };
    }
    
    private double confidenceLevel => Config.ConfidenceLevel;
}