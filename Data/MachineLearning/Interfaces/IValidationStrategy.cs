using MedjCap.Data.DataQuality.Models;
using MedjCap.Data.Trading.Models;
using MedjCap.Data.Analysis.Models;
using MedjCap.Data.TimeSeries.Models;
using MedjCap.Data.MachineLearning.Models;

namespace MedjCap.Data.MachineLearning.Interfaces;

/// <summary>
/// Strategy pattern interface for different validation approaches in ML boundary optimization.
/// Allows pluggable validation methods for model evaluation and selection.
/// </summary>
public interface IValidationStrategy
{
    /// <summary>
    /// Validates an optimization method using the strategy's approach.
    /// </summary>
    /// <param name="data">The price movement data to validate on</param>
    /// <param name="method">The optimization method to validate</param>
    /// <returns>Validation results with performance metrics</returns>
    CrossValidationResult Validate(List<PriceMovement> data, IOptimizationMethod method);
    
    /// <summary>
    /// Gets the validation strategy name for logging and diagnostics.
    /// </summary>
    string StrategyName { get; }
    
    /// <summary>
    /// Gets the validation configuration used by this strategy.
    /// </summary>
    CrossValidationConfig Config { get; }
}

/// <summary>
/// Abstraction for optimization methods that can be validated.
/// Represents different ML algorithms used for boundary discovery.
/// </summary>
public interface IOptimizationMethod
{
    /// <summary>
    /// Trains the optimization method on the provided data.
    /// </summary>
    /// <param name="trainingData">Training data for model fitting</param>
    /// <param name="config">Configuration parameters for training</param>
    /// <returns>Discovered optimal boundaries from training</returns>
    List<OptimalBoundary> Train(List<PriceMovement> trainingData, MLOptimizationConfig config);
    
    /// <summary>
    /// Evaluates the trained method on validation/test data.
    /// </summary>
    /// <param name="boundaries">Boundaries discovered during training</param>
    /// <param name="testData">Test data for evaluation</param>
    /// <param name="config">Configuration parameters for evaluation</param>
    /// <returns>Performance score (higher is better)</returns>
    double Evaluate(List<OptimalBoundary> boundaries, List<PriceMovement> testData, MLOptimizationConfig config);
    
    /// <summary>
    /// Gets the optimization method name for identification.
    /// </summary>
    string MethodName { get; }
}