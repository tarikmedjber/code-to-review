using MedjCap.Data.Domain;

namespace MedjCap.Data.Core;

/// <summary>
/// Interface for machine learning-based boundary optimization in financial indicator analysis.
/// Provides advanced ML algorithms to discover optimal measurement ranges for trading signals.
/// </summary>
public interface IMLBoundaryOptimizer
{
    // Basic Boundary Optimization
    /// <summary>
    /// Finds optimal measurement boundaries using machine learning algorithms to maximize predictive accuracy.
    /// Combines multiple ML approaches to identify ranges that consistently predict target ATR movements.
    /// </summary>
    /// <param name="movements">Historical price movement data with measurement values</param>
    /// <param name="targetATRMove">Target ATR movement threshold for signal generation</param>
    /// <param name="maxRanges">Maximum number of boundary ranges to discover</param>
    /// <returns>List of optimal boundaries with hit rates, confidence intervals, and statistical significance</returns>
    List<OptimalBoundary> FindOptimalBoundaries(List<PriceMovement> movements, decimal targetATRMove, int maxRanges);

    // Algorithmic Approaches
    /// <summary>
    /// Uses decision tree algorithms to find optimal boundary points based on measurement-to-ATR relationships.
    /// Identifies split points that maximize information gain for predicting price movements.
    /// </summary>
    /// <param name="movements">Training data for decision tree construction</param>
    /// <param name="maxDepth">Maximum depth of the decision tree to prevent overfitting</param>
    /// <returns>List of optimal boundary values derived from decision tree splits</returns>
    List<decimal> OptimizeWithDecisionTree(List<PriceMovement> movements, int maxDepth);
    
    /// <summary>
    /// Applies clustering algorithms to discover natural groupings in measurement-ATR space.
    /// Identifies measurement ranges with similar ATR movement characteristics.
    /// </summary>
    /// <param name="movements">Data points to cluster based on measurement and ATR values</param>
    /// <param name="numberOfClusters">Number of clusters to form</param>
    /// <returns>Cluster results with centroids, boundaries, and cluster statistics</returns>
    List<ClusterResult> OptimizeWithClustering(List<PriceMovement> movements, int numberOfClusters);
    
    /// <summary>
    /// Employs gradient-based optimization to find measurement ranges that maximize specified objectives.
    /// Uses numerical optimization techniques to converge on optimal boundary values.
    /// </summary>
    /// <param name="movements">Historical data for optimization</param>
    /// <param name="objective">Optimization objective (hit rate, profit factor, Sharpe ratio, etc.)</param>
    /// <returns>Optimal range with boundary values and objective function value</returns>
    OptimalRange OptimizeWithGradientSearch(List<PriceMovement> movements, OptimizationObjective objective);

    // Advanced Optimization
    /// <summary>
    /// Runs comprehensive optimization combining decision trees, clustering, and gradient search.
    /// Produces ensemble results with cross-validation and statistical significance testing.
    /// </summary>
    /// <param name="movements">Historical price movement data</param>
    /// <param name="config">Configuration specifying which algorithms to use and their parameters</param>
    /// <returns>Combined results from all enabled optimization algorithms with ensemble analysis</returns>
    CombinedOptimizationResult RunCombinedOptimization(List<PriceMovement> movements, MLOptimizationConfig config);
    
    /// <summary>
    /// Validates discovered boundaries using out-of-sample test data to assess generalization performance.
    /// Calculates hit rates, statistical significance, and overfitting metrics.
    /// </summary>
    /// <param name="boundaries">Boundary candidates to validate</param>
    /// <param name="testMovements">Independent test data for validation</param>
    /// <returns>Validation results with performance metrics and statistical tests</returns>
    ValidationResult ValidateBoundaries(List<OptimalBoundary> boundaries, List<PriceMovement> testMovements);
    
    /// <summary>
    /// Discovers dynamic boundaries that adapt over time using rolling window analysis.
    /// Identifies boundaries that perform consistently across different market conditions.
    /// </summary>
    /// <param name="movements">Time series data for dynamic analysis</param>
    /// <param name="windowSize">Size of each analysis window</param>
    /// <param name="stepSize">Step size for rolling the window</param>
    /// <returns>Time-varying boundary windows with performance tracking</returns>
    List<DynamicBoundaryWindow> FindDynamicBoundaries(List<PriceMovement> movements, int windowSize, int stepSize);
    
    /// <summary>
    /// Performs multi-objective optimization to find Pareto-optimal solutions balancing competing objectives.
    /// Discovers boundaries that optimize trade-offs between hit rate, profit factor, and risk metrics.
    /// </summary>
    /// <param name="movements">Historical data for optimization</param>
    /// <param name="objectives">List of objectives to optimize simultaneously</param>
    /// <returns>Pareto frontier solutions representing optimal trade-offs between objectives</returns>
    List<ParetoSolution> OptimizeForMultipleObjectives(List<PriceMovement> movements, List<OptimizationObjective> objectives);

    // Cross-Validation Methods
    /// <summary>
    /// Performs standard k-fold cross-validation for boundary optimization validation.
    /// Randomly splits data into k folds for robust performance estimation.
    /// </summary>
    /// <param name="data">Complete dataset for cross-validation</param>
    /// <param name="k">Number of folds (default: 5)</param>
    /// <returns>Cross-validation results with performance metrics across all folds</returns>
    CrossValidationResult KFoldCrossValidation(List<PriceMovement> data, int k = 5);
    
    /// <summary>
    /// Performs time-aware k-fold cross-validation respecting temporal order of financial data.
    /// Ensures training data always precedes validation data to prevent look-ahead bias.
    /// </summary>
    /// <param name="data">Time-ordered dataset for validation</param>
    /// <param name="k">Number of time-based folds (default: 5)</param>
    /// <returns>Time-series cross-validation results with temporal performance tracking</returns>
    CrossValidationResult TimeSeriesKFold(List<PriceMovement> data, int k = 5);
    
    /// <summary>
    /// Performs expanding window validation where training set grows over time while test set remains fixed.
    /// Simulates real-world scenario where more historical data becomes available over time.
    /// </summary>
    /// <param name="data">Time series data for expanding window analysis</param>
    /// <param name="initialSize">Initial training set size as fraction of total data</param>
    /// <param name="stepSize">Step size for expanding the training window</param>
    /// <returns>Expanding window validation results with performance evolution over time</returns>
    TimeSeriesCrossValidationResult ExpandingWindowValidation(List<PriceMovement> data, double initialSize, double stepSize);
    
    /// <summary>
    /// Performs rolling window validation with fixed-size training and test windows.
    /// Assesses boundary stability and adaptability to changing market conditions.
    /// </summary>
    /// <param name="data">Time series data for rolling window analysis</param>
    /// <param name="windowSize">Size of training/test windows as fraction of total data</param>
    /// <param name="stepSize">Step size for rolling the windows</param>
    /// <returns>Rolling window validation results with temporal performance patterns</returns>
    TimeSeriesCrossValidationResult RollingWindowValidation(List<PriceMovement> data, double windowSize, double stepSize);
}