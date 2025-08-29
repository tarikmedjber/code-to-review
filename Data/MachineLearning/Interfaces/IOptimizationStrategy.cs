using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Models;
using MedjCap.Data.MachineLearning.Optimization.Models;

namespace MedjCap.Data.MachineLearning.Interfaces;

/// <summary>
/// Strategy interface for different ML boundary optimization algorithms.
/// Enables pluggable optimization approaches following the Strategy pattern.
/// </summary>
public interface IOptimizationStrategy
{
    /// <summary>
    /// Name of the optimization strategy for identification and reporting.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Indicates whether this strategy is enabled and should be used.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Optimizes boundaries using the specific algorithm implementation.
    /// </summary>
    /// <param name="trainingData">Price movements for training the model</param>
    /// <param name="config">ML optimization configuration</param>
    /// <returns>Optimization result containing boundaries, score, and execution metrics</returns>
    OptimizationStrategyResult Optimize(List<PriceMovement> trainingData, MLOptimizationConfig config);

    /// <summary>
    /// Evaluates the performance of optimized boundaries on validation data.
    /// </summary>
    /// <param name="boundaries">Boundaries to evaluate</param>
    /// <param name="validationData">Price movements for validation</param>
    /// <param name="targetATRMove">Target ATR movement threshold</param>
    /// <returns>Performance score (0.0 to 1.0)</returns>
    double EvaluateBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> validationData, decimal targetATRMove);

    /// <summary>
    /// Gets strategy-specific configuration parameters.
    /// </summary>
    /// <returns>Dictionary of parameter names and values</returns>
    Dictionary<string, object> GetParameters();

    /// <summary>
    /// Validates that the training data is suitable for this optimization strategy.
    /// </summary>
    /// <param name="trainingData">Training data to validate</param>
    /// <param name="config">Optimization configuration</param>
    /// <returns>Validation result with any errors or warnings</returns>
    ValidationResult ValidateTrainingData(List<PriceMovement> trainingData, MLOptimizationConfig config);
}

/// <summary>
/// Result of an optimization strategy execution.
/// </summary>
public record OptimizationStrategyResult
{
    /// <summary>
    /// Optimal boundaries discovered by the strategy.
    /// </summary>
    public List<OptimalBoundary> Boundaries { get; init; } = new();

    /// <summary>
    /// Performance score achieved by the boundaries (0.0 to 1.0).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Time taken to execute the optimization.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Strategy-specific parameters used during optimization.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Any warnings or diagnostics generated during optimization.
    /// </summary>
    public List<string> Diagnostics { get; init; } = new();

    /// <summary>
    /// Whether the optimization completed successfully.
    /// </summary>
    public bool IsSuccessful { get; init; } = true;

    /// <summary>
    /// Error message if optimization failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Factory interface for creating optimization strategies.
/// </summary>
public interface IOptimizationStrategyFactory
{
    /// <summary>
    /// Creates all available optimization strategies based on configuration.
    /// </summary>
    /// <param name="config">ML optimization configuration</param>
    /// <returns>Collection of enabled optimization strategies</returns>
    IEnumerable<IOptimizationStrategy> CreateStrategies(MLOptimizationConfig config);

    /// <summary>
    /// Creates a specific optimization strategy by name.
    /// </summary>
    /// <param name="strategyName">Name of the strategy to create</param>
    /// <param name="config">ML optimization configuration</param>
    /// <returns>The requested strategy or null if not found</returns>
    IOptimizationStrategy? CreateStrategy(string strategyName, MLOptimizationConfig config);

    /// <summary>
    /// Gets names of all supported optimization strategies.
    /// </summary>
    /// <returns>Collection of strategy names</returns>
    IEnumerable<string> GetSupportedStrategies();
}