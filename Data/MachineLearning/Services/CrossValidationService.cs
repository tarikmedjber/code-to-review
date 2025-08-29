using MedjCap.Data.Infrastructure.Configuration.Options;
using MedjCap.Data.MachineLearning.Interfaces;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Infrastructure.Exceptions;
using MedjCap.Data.MachineLearning.Optimization.Models;
using MedjCap.Data.MachineLearning.Services.ValidationStrategies;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.MachineLearning.Services;

/// <summary>
/// Service for running cross-validation on ML boundary optimization methods.
/// Provides flexible validation strategies for model selection and overfitting detection.
/// </summary>
public class CrossValidationService
{
    private readonly ValidationConfig _config;
    private readonly StatisticalConfig _statisticalConfig;

    public CrossValidationService(IOptions<ValidationConfig> config, IOptions<StatisticalConfig> statisticalConfig)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _statisticalConfig = statisticalConfig?.Value ?? throw new ArgumentNullException(nameof(statisticalConfig));
    }

    /// <summary>
    /// Runs cross-validation using the specified strategy and optimization method.
    /// </summary>
    /// <param name="data">Price movement data for validation</param>
    /// <param name="strategy">Validation strategy to use</param>
    /// <param name="config">ML optimization configuration</param>
    /// <returns>Cross-validation results with performance metrics</returns>
    public CrossValidationResult RunCrossValidation(
        List<PriceMovement> data,
        IValidationStrategy strategy,
        MLOptimizationConfig config)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (data.Count < strategy.Config.KFolds)
        {
            throw new InsufficientDataException(
                "cross-validation",
                strategy.Config.KFolds,
                data.Count,
                $"Need at least {strategy.Config.KFolds} samples for {strategy.Config.KFolds}-fold cross-validation");
        }

        // Create optimization method wrapper
        var optimizationMethod = new BoundaryOptimizationMethod(config);
        
        try
        {
            var result = strategy.Validate(data, optimizationMethod);
            
            // Add additional validation metrics
            result = result with
            {
                Metrics = result.Metrics
                    .Concat(CalculateAdditionalMetrics(result, data, strategy))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return result;
        }
        catch (Exception ex) when (!(ex is MedjCapException))
        {
            throw new OptimizationConvergenceException(
                OptimizationTarget.HighestWinRate,
                0,
                strategy.Config.KFolds,
                double.NaN,
                0.001,
                ConvergenceFailureReason.AlgorithmError,
                errorHistory: null,
                $"Cross-validation failed using strategy '{strategy.StrategyName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates additional validation metrics beyond the basic strategy results.
    /// </summary>
    private Dictionary<string, double> CalculateAdditionalMetrics(
        CrossValidationResult result,
        List<PriceMovement> data,
        IValidationStrategy strategy)
    {
        var metrics = new Dictionary<string, double>();

        if (result.FoldResults.Any())
        {
            // Calculate bias-variance decomposition metrics
            var trainScores = result.FoldResults.Select(f => f.TrainingScore).ToList();
            var valScores = result.FoldResults.Select(f => f.ValidationScore).ToList();

            metrics["TrainMean"] = trainScores.Average();
            metrics["TrainStdDev"] = CalculateStdDev(trainScores);
            metrics["ValidationMean"] = valScores.Average();
            metrics["ValidationStdDev"] = CalculateStdDev(valScores);
            
            // Bias-variance tradeoff indicators
            var trainValGap = trainScores.Average() - valScores.Average();
            metrics["BiasVarianceGap"] = trainValGap;
            metrics["OverfittingRisk"] = Math.Max(0, trainValGap / Math.Max(trainScores.Average(), 0.001));
            
            // Stability metrics
            var cvStability = 1.0 - (result.StdDevScore / Math.Max(Math.Abs(result.MeanScore), 0.001));
            metrics["CVStability"] = Math.Max(0, Math.Min(1, cvStability));
            
            // Sample efficiency metrics
            var avgTrainSize = result.FoldResults.Average(f => f.TrainingSampleCount);
            var avgValSize = result.FoldResults.Average(f => f.ValidationSampleCount);
            metrics["AvgTrainSampleSize"] = avgTrainSize;
            metrics["AvgValSampleSize"] = avgValSize;
            metrics["SampleEfficiency"] = result.MeanScore / Math.Max(avgTrainSize, 1);
        }

        // Data quality metrics
        metrics["DataSize"] = data.Count;
        metrics["UniqueValues"] = data.Select(d => d.MeasurementValue).Distinct().Count();
        metrics["DataSparsity"] = (double)data.Select(d => d.MeasurementValue).Distinct().Count() / data.Count;

        return metrics;
    }

    /// <summary>
    /// Creates a K-fold cross-validation strategy.
    /// </summary>
    public IValidationStrategy CreateKFoldStrategy(int k = 5, int? randomSeed = null)
    {
        var config = new CrossValidationConfig
        {
            KFolds = k,
            Strategy = CrossValidationStrategy.KFold,
            RandomSeed = randomSeed,
            ConfidenceLevel = _statisticalConfig.DefaultConfidenceLevel
        };
        
        return new KFoldValidationStrategy(config, _statisticalConfig);
    }

    /// <summary>
    /// Creates a time-series expanding window validation strategy.
    /// </summary>
    public IValidationStrategy CreateExpandingWindowStrategy(
        double initialWindowSize = 0.3,
        double stepSize = 0.1)
    {
        var config = new CrossValidationConfig
        {
            Strategy = CrossValidationStrategy.TimeSeriesExpanding,
            MinimumTrainWindowSize = initialWindowSize,
            StepSize = stepSize,
            ConfidenceLevel = _statisticalConfig.DefaultConfidenceLevel
        };
        
        return new ExpandingWindowValidationStrategy(config, _statisticalConfig);
    }

    /// <summary>
    /// Creates a time-series rolling window validation strategy.
    /// </summary>
    public IValidationStrategy CreateRollingWindowStrategy(
        double windowSize = 0.5,
        double stepSize = 0.1)
    {
        var config = new CrossValidationConfig
        {
            Strategy = CrossValidationStrategy.TimeSeriesRolling,
            MinimumTrainWindowSize = windowSize,
            StepSize = stepSize,
            ConfidenceLevel = _statisticalConfig.DefaultConfidenceLevel
        };
        
        return new RollingWindowValidationStrategy(config, _statisticalConfig);
    }

    private double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var variance = values.Select(v => Math.Pow(v - mean, 2)).Sum() / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}

/// <summary>
/// Wrapper class that adapts the existing MLBoundaryOptimizer to the IOptimizationMethod interface.
/// </summary>
internal class BoundaryOptimizationMethod : IOptimizationMethod
{
    private readonly MLOptimizationConfig _config;
    public string MethodName => "BoundaryOptimization";

    public BoundaryOptimizationMethod(MLOptimizationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public List<OptimalBoundary> Train(List<PriceMovement> trainingData, MLOptimizationConfig config)
    {
        // Use the existing FindOptimalBoundaries method for training
        // This will be delegated to the actual MLBoundaryOptimizer instance
        // For now, return empty list - this will be properly implemented when integrated
        return new List<OptimalBoundary>();
    }

    public double Evaluate(List<OptimalBoundary> boundaries, List<PriceMovement> testData, MLOptimizationConfig config)
    {
        if (!boundaries.Any() || !testData.Any())
            return 0.0;

        // Calculate hit rate as performance metric
        var totalHits = 0;
        var totalSamples = 0;

        foreach (var boundary in boundaries)
        {
            var movementsInRange = testData
                .Where(m => m.MeasurementValue >= boundary.RangeLow && m.MeasurementValue <= boundary.RangeHigh)
                .ToList();

            if (movementsInRange.Any())
            {
                var targetATR = boundary.ExpectedATRMove;
                var hits = movementsInRange.Count(m => Math.Abs(m.ATRMovement) >= targetATR);
                
                totalHits += hits;
                totalSamples += movementsInRange.Count;
            }
        }

        return totalSamples > 0 ? (double)totalHits / totalSamples : 0.0;
    }
}