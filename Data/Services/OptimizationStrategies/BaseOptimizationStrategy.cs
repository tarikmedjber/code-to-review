using MedjCap.Data.Configuration;
using MedjCap.Data.Core;
using MedjCap.Data.Domain;
using Microsoft.Extensions.Options;

namespace MedjCap.Data.Services.OptimizationStrategies;

/// <summary>
/// Abstract base class for optimization strategies providing common functionality.
/// </summary>
public abstract class BaseOptimizationStrategy : IOptimizationStrategy
{
    protected readonly OptimizationConfig _optimizationConfig;

    protected BaseOptimizationStrategy(IOptions<OptimizationConfig> optimizationConfig)
    {
        _optimizationConfig = optimizationConfig?.Value ?? throw new ArgumentNullException(nameof(optimizationConfig));
    }

    /// <summary>
    /// Name of the optimization strategy for identification and reporting.
    /// </summary>
    public abstract string StrategyName { get; }

    /// <summary>
    /// Indicates whether this strategy is enabled and should be used.
    /// </summary>
    public abstract bool IsEnabled { get; }

    /// <summary>
    /// Optimizes boundaries using the specific algorithm implementation.
    /// </summary>
    public OptimizationStrategyResult Optimize(List<PriceMovement> trainingData, MLOptimizationConfig config)
    {
        var startTime = DateTime.UtcNow;
        var diagnostics = new List<string>();

        try
        {
            // Validate training data
            var validationResult = ValidateTrainingData(trainingData, config);
            if (!IsValidationSuccessful(validationResult))
            {
                return new OptimizationStrategyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Training data validation failed: {string.Join(", ", GetValidationErrors(validationResult))}",
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }

            // Add any validation warnings to diagnostics
            diagnostics.AddRange(GetValidationWarnings(validationResult));

            // Execute strategy-specific optimization
            var boundaries = ExecuteOptimization(trainingData, config, diagnostics);
            
            // Calculate performance score (placeholder - derived classes should override if needed)
            var score = CalculateDefaultScore(boundaries, trainingData, config.TargetATRMove);

            return new OptimizationStrategyResult
            {
                Boundaries = boundaries,
                Score = score,
                ExecutionTime = DateTime.UtcNow - startTime,
                Parameters = GetParameters(),
                Diagnostics = diagnostics,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            return new OptimizationStrategyResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                Diagnostics = diagnostics
            };
        }
    }

    /// <summary>
    /// Evaluates the performance of optimized boundaries on validation data.
    /// </summary>
    public virtual double EvaluateBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> validationData, decimal targetATRMove)
    {
        if (!boundaries.Any() || !validationData.Any())
            return 0.0;

        var totalScore = 0.0;
        var validBoundaries = 0;

        foreach (var boundary in boundaries)
        {
            var movementsInRange = validationData.Where(m => 
                m.MeasurementValue >= boundary.RangeLow && 
                m.MeasurementValue <= boundary.RangeHigh).ToList();

            if (!movementsInRange.Any()) continue;

            var largeMoves = movementsInRange.Count(m => Math.Abs(m.ATRMovement) >= targetATRMove);
            var hitRate = (double)largeMoves / movementsInRange.Count;
            
            totalScore += hitRate;
            validBoundaries++;
        }

        return validBoundaries > 0 ? totalScore / validBoundaries : 0.0;
    }

    /// <summary>
    /// Gets strategy-specific configuration parameters.
    /// </summary>
    public abstract Dictionary<string, object> GetParameters();

    /// <summary>
    /// Validates that the training data is suitable for this optimization strategy.
    /// </summary>
    public virtual ValidationResult ValidateTrainingData(List<PriceMovement> trainingData, MLOptimizationConfig config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var context = new Dictionary<string, object>();

        // Common validations
        if (trainingData == null || !trainingData.Any())
        {
            errors.Add("Training data cannot be null or empty");
        }
        else
        {
            context["SampleSize"] = trainingData.Count;
            
            // Check minimum sample size
            if (trainingData.Count < GetMinimumSampleSize())
            {
                errors.Add($"Insufficient training data. Required: {GetMinimumSampleSize()}, Actual: {trainingData.Count}");
            }
            else if (trainingData.Count < GetRecommendedSampleSize())
            {
                warnings.Add($"Training data size below recommended threshold. Recommended: {GetRecommendedSampleSize()}, Actual: {trainingData.Count}");
            }

            // Check for data variety
            var uniqueValues = trainingData.Select(m => m.MeasurementValue).Distinct().Count();
            if (uniqueValues < 3)
            {
                errors.Add("Training data must contain at least 3 unique measurement values for meaningful optimization");
            }

            // Strategy-specific validation
            PerformStrategySpecificValidation(trainingData, config, errors, warnings, context);
        }

        return new ValidationResult
        {
            ValidationMetrics = new Dictionary<string, double>
            {
                ["ErrorCount"] = errors.Count,
                ["WarningCount"] = warnings.Count,
                ["IsValid"] = errors.Count == 0 ? 1.0 : 0.0
            }
        };
    }

    /// <summary>
    /// Executes the strategy-specific optimization algorithm.
    /// </summary>
    /// <param name="trainingData">Validated training data</param>
    /// <param name="config">ML optimization configuration</param>
    /// <param name="diagnostics">List to add diagnostic messages to</param>
    /// <returns>Optimized boundaries</returns>
    protected abstract List<OptimalBoundary> ExecuteOptimization(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config, 
        List<string> diagnostics);

    /// <summary>
    /// Gets the minimum sample size required for this strategy.
    /// </summary>
    protected abstract int GetMinimumSampleSize();

    /// <summary>
    /// Gets the recommended sample size for optimal performance.
    /// </summary>
    protected virtual int GetRecommendedSampleSize() => GetMinimumSampleSize() * 3;

    /// <summary>
    /// Performs strategy-specific validation beyond common checks.
    /// </summary>
    protected virtual void PerformStrategySpecificValidation(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config,
        List<string> errors, 
        List<string> warnings, 
        Dictionary<string, object> context)
    {
        // Override in derived classes for specific validation logic
    }

    /// <summary>
    /// Calculates a default performance score for boundaries.
    /// </summary>
    protected virtual double CalculateDefaultScore(List<OptimalBoundary> boundaries, List<PriceMovement> trainingData, decimal targetATRMove)
    {
        return EvaluateBoundaries(boundaries, trainingData, targetATRMove);
    }

    private bool IsValidationSuccessful(ValidationResult result)
    {
        return result.ValidationMetrics.GetValueOrDefault("IsValid", 0) == 1.0;
    }

    private List<string> GetValidationErrors(ValidationResult result)
    {
        // Simplified error extraction - in a real implementation, would extract from result structure
        var errorCount = (int)result.ValidationMetrics.GetValueOrDefault("ErrorCount", 0);
        return errorCount > 0 
            ? new List<string> { $"{errorCount} validation error(s) found" }
            : new List<string>();
    }

    private List<string> GetValidationWarnings(ValidationResult result)
    {
        // Simplified warning extraction - in a real implementation, would extract from result structure  
        var warningCount = (int)result.ValidationMetrics.GetValueOrDefault("WarningCount", 0);
        return warningCount > 0 
            ? new List<string> { $"{warningCount} validation warning(s) found" }
            : new List<string>();
    }
}