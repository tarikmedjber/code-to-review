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
/// Optimization strategy using gradient-based search to find optimal measurement value ranges.
/// Uses numerical optimization to maximize objective functions (hit rate, profitability, etc.).
/// </summary>
public class GradientSearchOptimizationStrategy : BaseOptimizationStrategy
{
    private readonly int _maxIterations;
    private readonly double _convergenceThreshold;
    private readonly double _learningRate;
    private readonly bool _isEnabled;

    public GradientSearchOptimizationStrategy(
        IOptions<OptimizationConfig> optimizationConfig,
        IOptions<MLOptimizationConfig> mlConfig) 
        : base(optimizationConfig)
    {
        var config = mlConfig?.Value ?? throw new ArgumentNullException(nameof(mlConfig));
        _maxIterations = GetParameterValue<int>(config.AlgorithmParameters, "GradientMaxIterations") ?? config.MaxIterations;
        _convergenceThreshold = GetParameterValue<double>(config.AlgorithmParameters, "GradientConvergenceThreshold") ?? config.ConvergenceThreshold;
        _learningRate = GetParameterValue<double>(config.AlgorithmParameters, "GradientLearningRate") ?? 0.01;
        _isEnabled = config.UseGradientSearch;
    }

    /// <summary>
    /// Name of the optimization strategy for identification and reporting.
    /// </summary>
    public override string StrategyName => "GradientSearch";

    /// <summary>
    /// Indicates whether this strategy is enabled and should be used.
    /// </summary>
    public override bool IsEnabled => _isEnabled;

    /// <summary>
    /// Gets strategy-specific configuration parameters.
    /// </summary>
    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            ["MaxIterations"] = _maxIterations,
            ["ConvergenceThreshold"] = _convergenceThreshold,
            ["LearningRate"] = _learningRate,
            ["IsEnabled"] = _isEnabled,
            ["Algorithm"] = "Gradient Descent"
        };
    }

    /// <summary>
    /// Gets the minimum sample size required for gradient search optimization.
    /// </summary>
    protected override int GetMinimumSampleSize() => 30; // Need reasonable sample for gradient estimation

    /// <summary>
    /// Gets the recommended sample size for optimal gradient search performance.
    /// </summary>
    protected override int GetRecommendedSampleSize() => 100; // More data for stable gradients

    /// <summary>
    /// Performs gradient search specific validation.
    /// </summary>
    protected override void PerformStrategySpecificValidation(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config,
        List<string> errors, 
        List<string> warnings, 
        Dictionary<string, object> context)
    {
        // Check data distribution for gradient optimization
        var measurementValues = trainingData.Select(m => m.MeasurementValue).OrderBy(x => x).ToList();
        var dataRange = measurementValues.Last() - measurementValues.First();
        
        if (dataRange == 0)
        {
            errors.Add("All measurement values are identical - gradient search cannot optimize boundaries");
        }

        // Check for sufficient variation in target variable
        var largeMoves = trainingData.Count(m => Math.Abs(m.ATRMovement) >= config.TargetATRMove);
        var smallMoves = trainingData.Count - largeMoves;

        if (largeMoves == 0 || smallMoves == 0)
        {
            errors.Add("All price movements are of the same magnitude - gradient search has no variation to optimize");
        }
        else
        {
            var imbalanceRatio = Math.Min(largeMoves, smallMoves) / (double)Math.Max(largeMoves, smallMoves);
            if (imbalanceRatio < 0.1)
            {
                warnings.Add($"Highly imbalanced data (ratio: {imbalanceRatio:F2}) may cause gradient search instability");
            }
        }

        // Validate learning rate isn't too aggressive for the data scale
        var typicalValueRange = (double)(measurementValues[(int)(measurementValues.Count * 0.75)] - measurementValues[(int)(measurementValues.Count * 0.25)]);
        if (_learningRate * typicalValueRange > 1.0)
        {
            warnings.Add($"Learning rate {_learningRate} may be too aggressive for data scale (typical range: {typicalValueRange:F2})");
        }

        context["DataRange"] = dataRange;
        context["ImbalanceRatio"] = largeMoves + smallMoves > 0 ? Math.Min(largeMoves, smallMoves) / (double)Math.Max(largeMoves, smallMoves) : 0;
        context["TypicalValueRange"] = typicalValueRange;
    }

    /// <summary>
    /// Executes gradient-based optimization to find optimal boundaries.
    /// </summary>
    protected override List<OptimalBoundary> ExecuteOptimization(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config, 
        List<string> diagnostics)
    {
        diagnostics.Add($"Starting gradient search optimization with learning rate {_learningRate}");

        // Initialize optimization parameters
        var sortedData = trainingData.OrderBy(m => m.MeasurementValue).ToList();
        var minValue = (double)sortedData.First().MeasurementValue;
        var maxValue = (double)sortedData.Last().MeasurementValue;
        var dataRange = maxValue - minValue;

        // Initialize multiple starting points for robustness
        var numRanges = Math.Min(5, trainingData.Count / 20); // Up to 5 ranges, but need at least 20 samples per range
        var boundaries = new List<OptimalBoundary>();

        diagnostics.Add($"Optimizing {numRanges} boundary ranges across data range [{minValue:F2}, {maxValue:F2}]");

        // Optimize each range independently
        for (int rangeIndex = 0; rangeIndex < numRanges; rangeIndex++)
        {
            // Initialize range boundaries
            var rangeSize = dataRange / (numRanges + 1);
            var initialLower = minValue + rangeIndex * rangeSize;
            var initialUpper = initialLower + rangeSize * 1.5; // Overlapping ranges for better coverage

            var optimizedBoundary = OptimizeSingleRange(
                trainingData, 
                config.TargetATRMove, 
                initialLower, 
                initialUpper, 
                minValue, 
                maxValue, 
                diagnostics);

            if (optimizedBoundary != null)
            {
                boundaries.Add(optimizedBoundary);
            }
        }

        // Remove overlapping boundaries and keep the best ones
        boundaries = RemoveOverlappingBoundaries(boundaries);
        
        diagnostics.Add($"Generated {boundaries.Count} optimized boundaries after overlap removal");

        return boundaries.OrderBy(b => b.RangeLow).ToList();
    }

    /// <summary>
    /// Optimizes a single boundary range using gradient descent.
    /// </summary>
    private OptimalBoundary? OptimizeSingleRange(
        List<PriceMovement> trainingData,
        decimal targetATRMove,
        double initialLower,
        double initialUpper,
        double minValue,
        double maxValue,
        List<string> diagnostics)
    {
        var lowerBound = initialLower;
        var upperBound = initialUpper;
        
        var bestScore = 0.0;
        var bestLower = lowerBound;
        var bestUpper = upperBound;
        
        var previousScore = double.NegativeInfinity;
        var convergenceCount = 0;

        for (int iteration = 0; iteration < _maxIterations; iteration++)
        {
            // Ensure bounds are valid
            lowerBound = Math.Max(minValue, Math.Min(lowerBound, upperBound - 0.01));
            upperBound = Math.Min(maxValue, Math.Max(upperBound, lowerBound + 0.01));

            // Calculate current objective function value
            var currentScore = CalculateObjectiveFunction(trainingData, (decimal)lowerBound, (decimal)upperBound, targetATRMove);

            // Update best solution
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestLower = lowerBound;
                bestUpper = upperBound;
            }

            // Check for convergence
            if (Math.Abs(currentScore - previousScore) < _convergenceThreshold)
            {
                convergenceCount++;
                if (convergenceCount >= 5) // Require 5 consecutive converged iterations
                {
                    diagnostics.Add($"Range optimization converged at iteration {iteration} with score {bestScore:F4}");
                    break;
                }
            }
            else
            {
                convergenceCount = 0;
            }

            // Calculate gradients numerically
            var gradients = CalculateNumericalGradients(trainingData, lowerBound, upperBound, targetATRMove);
            
            // Update bounds using gradient descent
            lowerBound += _learningRate * gradients.LowerGradient;
            upperBound += _learningRate * gradients.UpperGradient;

            previousScore = currentScore;
        }

        // Only return boundary if it's meaningful
        if (bestScore > 0.1) // Minimum performance threshold
        {
            var dataInRange = trainingData.Where(m => 
                m.MeasurementValue >= (decimal)bestLower && m.MeasurementValue <= (decimal)bestUpper).ToList();

            return new OptimalBoundary
            {
                RangeLow = (decimal)bestLower,
                RangeHigh = (decimal)bestUpper,
                HitRate = bestScore,
                SampleCount = dataInRange.Count,
                Confidence = CalculateGradientConfidence(dataInRange.Count, bestScore),
                Method = "Gradient Search"
            };
        }

        return null;
    }

    /// <summary>
    /// Calculates the objective function value for given boundaries.
    /// </summary>
    private double CalculateObjectiveFunction(
        List<PriceMovement> data, 
        decimal lowerBound, 
        decimal upperBound, 
        decimal targetATRMove)
    {
        var dataInRange = data.Where(m => 
            m.MeasurementValue >= lowerBound && m.MeasurementValue <= upperBound).ToList();

        if (dataInRange.Count < 5) return 0.0; // Insufficient data

        var largeMoves = dataInRange.Count(m => Math.Abs(m.ATRMovement) >= targetATRMove);
        var hitRate = (double)largeMoves / dataInRange.Count;

        // Objective function: hit rate weighted by sample size
        var sizeWeight = Math.Min(1.0, dataInRange.Count / 30.0);
        return hitRate * sizeWeight;
    }

    /// <summary>
    /// Calculates numerical gradients for boundary optimization.
    /// </summary>
    private (double LowerGradient, double UpperGradient) CalculateNumericalGradients(
        List<PriceMovement> data, 
        double lowerBound, 
        double upperBound, 
        decimal targetATRMove)
    {
        const double epsilon = 0.01; // Small step for numerical differentiation

        var baseScore = CalculateObjectiveFunction(data, (decimal)lowerBound, (decimal)upperBound, targetATRMove);
        
        // Gradient for lower bound
        var lowerPlusScore = CalculateObjectiveFunction(data, (decimal)(lowerBound + epsilon), (decimal)upperBound, targetATRMove);
        var lowerMinusScore = CalculateObjectiveFunction(data, (decimal)(lowerBound - epsilon), (decimal)upperBound, targetATRMove);
        var lowerGradient = (lowerPlusScore - lowerMinusScore) / (2.0 * epsilon);

        // Gradient for upper bound
        var upperPlusScore = CalculateObjectiveFunction(data, (decimal)lowerBound, (decimal)(upperBound + epsilon), targetATRMove);
        var upperMinusScore = CalculateObjectiveFunction(data, (decimal)lowerBound, (decimal)(upperBound - epsilon), targetATRMove);
        var upperGradient = (upperPlusScore - upperMinusScore) / (2.0 * epsilon);

        return (lowerGradient, upperGradient);
    }

    /// <summary>
    /// Removes overlapping boundaries and keeps the best performing ones.
    /// </summary>
    private List<OptimalBoundary> RemoveOverlappingBoundaries(List<OptimalBoundary> boundaries)
    {
        if (boundaries.Count <= 1) return boundaries;

        var sortedBoundaries = boundaries.OrderBy(b => b.RangeLow).ToList();
        var nonOverlappingBoundaries = new List<OptimalBoundary> { sortedBoundaries[0] };

        for (int i = 1; i < sortedBoundaries.Count; i++)
        {
            var current = sortedBoundaries[i];
            var previous = nonOverlappingBoundaries.Last();

            // Check for overlap
            if (current.RangeLow < previous.RangeHigh)
            {
                // Keep the better performing boundary
                if (current.HitRate > previous.HitRate)
                {
                    nonOverlappingBoundaries[nonOverlappingBoundaries.Count - 1] = current;
                }
            }
            else
            {
                nonOverlappingBoundaries.Add(current);
            }
        }

        return nonOverlappingBoundaries;
    }

    /// <summary>
    /// Calculates confidence level for gradient search results.
    /// </summary>
    private double CalculateGradientConfidence(int sampleSize, double hitRate)
    {
        var sizeConfidence = Math.Min(1.0, sampleSize / 50.0);
        var performanceConfidence = Math.Min(1.0, hitRate * 1.5);
        return (sizeConfidence + performanceConfidence) / 2.0;
    }

    /// <summary>
    /// Gets a parameter value from the algorithm parameters dictionary.
    /// </summary>
    private T? GetParameterValue<T>(Dictionary<string, object> parameters, string key) where T : struct
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return null;
    }
}