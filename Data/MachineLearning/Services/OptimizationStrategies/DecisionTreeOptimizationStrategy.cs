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
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;

namespace MedjCap.Data.MachineLearning.Services.OptimizationStrategies;

/// <summary>
/// Optimization strategy using decision trees to find optimal measurement value boundaries.
/// Uses recursive partitioning to identify ranges with high probability of large price movements.
/// </summary>
public class DecisionTreeOptimizationStrategy : BaseOptimizationStrategy
{
    private readonly int _maxDepth;
    private readonly int _minimumSamplesPerLeaf;
    private readonly bool _isEnabled;

    public DecisionTreeOptimizationStrategy(
        IOptions<OptimizationConfig> optimizationConfig,
        IOptions<MLOptimizationConfig> mlConfig) 
        : base(optimizationConfig)
    {
        var config = mlConfig?.Value ?? throw new ArgumentNullException(nameof(mlConfig));
        _maxDepth = Math.Min(GetParameterValue<int>(config.AlgorithmParameters, "DecisionTreeMaxDepth") ?? 5, _optimizationConfig.MaxDepth);
        _minimumSamplesPerLeaf = GetParameterValue<int>(config.AlgorithmParameters, "DecisionTreeMinSamplesPerLeaf") ?? 10;
        _isEnabled = config.UseDecisionTree;
    }

    /// <summary>
    /// Name of the optimization strategy for identification and reporting.
    /// </summary>
    public override string StrategyName => "DecisionTree";

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
            ["MaxDepth"] = _maxDepth,
            ["MinimumSamplesPerLeaf"] = _minimumSamplesPerLeaf,
            ["IsEnabled"] = _isEnabled,
            ["Algorithm"] = "Accord.NET DecisionTree"
        };
    }

    /// <summary>
    /// Gets the minimum sample size required for decision tree optimization.
    /// </summary>
    protected override int GetMinimumSampleSize() => _minimumSamplesPerLeaf * 4; // At least 4 leaves worth of data

    /// <summary>
    /// Gets the recommended sample size for optimal decision tree performance.
    /// </summary>
    protected override int GetRecommendedSampleSize() => GetMinimumSampleSize() * 5; // Much more data for robust trees

    /// <summary>
    /// Performs decision tree specific validation.
    /// </summary>
    protected override void PerformStrategySpecificValidation(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config,
        List<string> errors, 
        List<string> warnings, 
        Dictionary<string, object> context)
    {
        // Check data distribution for decision tree suitability
        var measurementValues = trainingData.Select(m => m.MeasurementValue).ToList();
        var minValue = measurementValues.Min();
        var maxValue = measurementValues.Max();
        
        if (maxValue - minValue == 0)
        {
            errors.Add("All measurement values are identical - decision tree cannot create meaningful splits");
        }

        // Check if we have both large and small price movements for binary classification
        var largeMoves = trainingData.Count(m => Math.Abs(m.ATRMovement) >= config.TargetATRMove);
        var smallMoves = trainingData.Count - largeMoves;

        if (largeMoves == 0)
        {
            errors.Add("No large price movements found in training data - decision tree has no positive examples to learn from");
        }
        else if (smallMoves == 0)
        {
            warnings.Add("No small price movements found - decision tree may overfit to positive examples");
        }
        else if (largeMoves < _minimumSamplesPerLeaf || smallMoves < _minimumSamplesPerLeaf)
        {
            warnings.Add($"Imbalanced dataset may lead to poor decision tree performance. Large moves: {largeMoves}, Small moves: {smallMoves}");
        }

        context["LargeMovements"] = largeMoves;
        context["SmallMovements"] = smallMoves;
        context["ValueRange"] = maxValue - minValue;
    }

    /// <summary>
    /// Executes decision tree optimization to find optimal boundaries.
    /// </summary>
    protected override List<OptimalBoundary> ExecuteOptimization(
        List<PriceMovement> trainingData, 
        MLOptimizationConfig config, 
        List<string> diagnostics)
    {
        diagnostics.Add($"Starting decision tree optimization with max depth {_maxDepth}");

        // Prepare input features (measurement values) and labels (large move indicators)
        var inputs = trainingData.Select(m => new[] { (double)m.MeasurementValue }).ToArray();
        var outputs = trainingData.Select(m => Math.Abs(m.ATRMovement) >= config.TargetATRMove ? 1 : 0).ToArray();

        diagnostics.Add($"Prepared {inputs.Length} training samples with {outputs.Count(o => o == 1)} positive examples");

        // Create and train decision tree
        var teacher = new C45Learning() { MaxHeight = _maxDepth };
        var tree = teacher.Learn(inputs, outputs);
        
        diagnostics.Add($"Decision tree trained successfully with {CountNodes(tree)} nodes");

        // Extract split points from the trained tree
        var splitPoints = ExtractSplitPoints(tree);
        diagnostics.Add($"Extracted {splitPoints.Count} split points from decision tree");

        // Convert split points to boundaries
        var boundaries = ConvertSplitPointsToBoundaries(splitPoints, trainingData, config.TargetATRMove);
        diagnostics.Add($"Generated {boundaries.Count} optimal boundaries");

        return boundaries;
    }

    /// <summary>
    /// Extracts split points from a trained decision tree.
    /// </summary>
    private List<decimal> ExtractSplitPoints(DecisionTree tree)
    {
        var splitPoints = new List<decimal>();
        ExtractSplitPointsRecursive(tree.Root, splitPoints);
        return splitPoints.Distinct().OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Recursively extracts split points from decision tree nodes.
    /// </summary>
    private void ExtractSplitPointsRecursive(DecisionNode node, List<decimal> splitPoints)
    {
        if (node == null) return;

        // If this is a decision node (not a leaf), it has a threshold
        if (!node.IsLeaf && node.Branches != null)
        {
            splitPoints.Add((decimal)(node.Value ?? 0));
            
            // Recursively process child nodes
            foreach (var branch in node.Branches)
            {
                ExtractSplitPointsRecursive(branch, splitPoints);
            }
        }
    }

    /// <summary>
    /// Converts split points to optimal boundaries with performance metrics.
    /// </summary>
    private List<OptimalBoundary> ConvertSplitPointsToBoundaries(
        List<decimal> splitPoints, 
        List<PriceMovement> trainingData, 
        decimal targetATRMove)
    {
        var boundaries = new List<OptimalBoundary>();
        
        // Sort data for range processing
        var sortedData = trainingData.OrderBy(m => m.MeasurementValue).ToList();
        var allValues = sortedData.Select(m => m.MeasurementValue).ToList();
        
        // Create boundaries between consecutive split points
        for (int i = 0; i < splitPoints.Count - 1; i++)
        {
            var lowerBound = splitPoints[i];
            var upperBound = splitPoints[i + 1];
            
            // Find data points in this range
            var dataInRange = sortedData.Where(m => 
                m.MeasurementValue >= lowerBound && m.MeasurementValue <= upperBound).ToList();
            
            if (dataInRange.Count < _minimumSamplesPerLeaf) continue;
            
            // Calculate performance metrics for this range
            var largeMoves = dataInRange.Count(m => Math.Abs(m.ATRMovement) >= targetATRMove);
            var hitRate = dataInRange.Count > 0 ? (double)largeMoves / dataInRange.Count : 0.0;
            
            // Only include ranges with above-average performance
            if (hitRate > 0.1) // Configurable threshold
            {
                boundaries.Add(new OptimalBoundary
                {
                    RangeLow = lowerBound,
                    RangeHigh = upperBound,
                    HitRate = hitRate,
                    SampleCount = dataInRange.Count,
                    Confidence = CalculateConfidence(dataInRange.Count, hitRate),
                    Method = "DecisionTree"
                });
            }
        }

        return boundaries;
    }

    /// <summary>
    /// Calculates confidence level based on sample size and hit rate.
    /// </summary>
    private double CalculateConfidence(int sampleSize, double hitRate)
    {
        // Simple confidence calculation - could be enhanced with proper statistical methods
        var confidence = Math.Min(1.0, sampleSize / 100.0); // More samples = higher confidence
        var performanceConfidence = hitRate > 0.5 ? hitRate : 0.5; // Better performance = higher confidence
        return (confidence + performanceConfidence) / 2.0;
    }

    /// <summary>
    /// Counts the total number of nodes in a decision tree.
    /// </summary>
    private int CountNodes(DecisionTree tree)
    {
        return CountNodesRecursive(tree.Root);
    }

    /// <summary>
    /// Recursively counts nodes in a decision tree.
    /// </summary>
    private int CountNodesRecursive(DecisionNode node)
    {
        if (node == null) return 0;
        
        int count = 1; // Count this node
        
        if (node.Branches != null)
        {
            foreach (var branch in node.Branches)
            {
                count += CountNodesRecursive(branch);
            }
        }
        
        return count;
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